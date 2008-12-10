Imports Worm
Imports Worm.Sorting
Imports System.Collections.Generic
Imports Worm.Entities
Imports Worm.Criteria.Core
Imports Worm.Cache
Imports Worm.Entities.Query
Imports Worm.Entities.Meta
Imports Worm.Criteria.Joins
Imports Worm.Criteria.Values
Imports cc = Worm.Criteria.Core
Imports Worm.Criteria.Conditions
Imports System.Collections.ObjectModel

Namespace Database
    Partial Public Class OrmReadOnlyDBManager
        Inherits OrmManager

        Protected Friend Enum ConnAction
            Leave
            Destroy
            Close
        End Enum

        Public Class DBUpdater

            Public Function UpdateObject(ByVal mgr As OrmReadOnlyDBManager, ByVal obj As _ICachedEntity) As Boolean
                mgr.Invariant()

                If obj Is Nothing Then
                    Throw New ArgumentNullException("obj parameter cannot be nothing")
                End If

                'Assert(obj.ObjectState = ObjectState.Modified , "Object " & obj.ObjName & " should be in Modified state")
                'Dim t As Type = obj.GetType

                Dim params As IEnumerable(Of System.Data.Common.DbParameter) = Nothing
                Dim cols As Generic.IList(Of ColumnAttribute) = Nothing
                Dim upd As IList(Of Worm.Criteria.Core.EntityFilter) = Nothing
                Dim inv As Boolean
                Using obj.GetSyncRoot()
                    Dim cmdtext As String = Nothing
                    Try
                        cmdtext = mgr.SQLGenerator.Update(mgr.MappingEngine, obj, mgr.GetFilterInfo, params, cols, upd)
                    Catch ex As ObjectMappingException When ex.Message.Contains("Cannot save object while it has reference to new object")
                        Return False
                    End Try
                    If cmdtext.Length > 0 Then
                        If mgr.SQLGenerator.SupportMultiline Then
                            Using cmd As System.Data.Common.DbCommand = mgr.CreateDBCommand()
                                With cmd
                                    .CommandType = System.Data.CommandType.Text
                                    .CommandText = cmdtext
                                    For Each p As System.Data.Common.DbParameter In params
                                        .Parameters.Add(p)
                                    Next
                                End With

                                Dim b As ConnAction = mgr.TestConn(cmd)
                                Try
                                    mgr.LoadSingleObject(cmd, cols, obj, False, False, False)

                                    inv = True
                                Finally
                                    mgr.CloseConn(b)
                                End Try

                            End Using
                        Else
                            Dim tran As System.Data.Common.DbTransaction = mgr.Transaction
                            mgr.BeginTransaction()
                            Try
                                Dim prev_error As Boolean = False
                                For Each stmt As String In Microsoft.VisualBasic.Split(cmdtext, mgr.SQLGenerator.EndLine)
                                    If stmt = String.Empty Then Continue For
                                    Using cmd As System.Data.Common.DbCommand = mgr.CreateDBCommand()
                                        Dim sel As Boolean = stmt.IndexOf("select") >= 0
                                        With cmd
                                            .CommandType = System.Data.CommandType.Text
                                            .CommandText = stmt
                                            Dim p As IList(Of System.Data.Common.DbParameter) = CType(params, Global.System.Collections.Generic.IList(Of Global.System.Data.Common.DbParameter))
                                            For i As Integer = 0 To ExtractParamsCount(stmt) - 1
                                                .Parameters.Add(CType(p(0), System.Data.Common.DbParameter))
                                                p.RemoveAt(0)
                                            Next
                                        End With

                                        If stmt.StartsWith("{{error}}") Then
                                            If prev_error Then
                                                cmd.CommandText = stmt.Remove(0, 9).Trim
                                            Else
                                                Continue For
                                            End If
                                        ElseIf prev_error Then
                                            Throw mgr.SQLGenerator.PrepareConcurrencyException(mgr.MappingEngine, obj)
                                        End If

                                        prev_error = False
                                        Dim b As ConnAction = mgr.TestConn(cmd)
                                        Try
                                            If sel Then
                                                mgr.LoadSingleObject(cmd, cols, obj, False, False, False)
                                            Else
                                                Dim r As Integer = cmd.ExecuteNonQuery()
                                                If r = 0 Then
                                                    prev_error = True
                                                    If _mcSwitch.TraceWarning Then
                                                        WriteLine(cmd.CommandText & " affected 0 rows!")
                                                    End If
                                                    'Debug.WriteLine(Environment.StackTrace.ToString)
                                                End If
                                            End If
                                        Finally
                                            mgr.CloseConn(b)
                                        End Try
                                    End Using
                                Next

                                inv = True
                            Finally
                                If tran Is Nothing Then
                                    mgr.Commit()
                                End If
                            End Try
                        End If
                    End If

                    If inv Then
                        obj.UpdateCtx.UpdatedFields = upd
                        'Это было вне юзинга
                        'InvalidateCache(obj, CType(upd, System.Collections.ICollection))
                    End If
                End Using
                Return True
            End Function

            Public Function InsertObject(ByVal mgr As OrmReadOnlyDBManager, ByVal obj As _ICachedEntity) As Boolean
                mgr.Invariant()

                If obj Is Nothing Then
                    Throw New ArgumentNullException("obj parameter cannot be nothing")
                End If

                Assert(obj.ObjectState = ObjectState.Created, "Object " & obj.ObjName & " should be in Created state")

                Dim oldl As Boolean = obj.IsLoaded
                Dim err As Boolean = True
                Try
                    'obj.IsLoaded = True

                    'Dim t As Type = obj.GetType

                    Dim params As ICollection(Of System.Data.Common.DbParameter) = Nothing
                    Dim cols As Generic.IList(Of ColumnAttribute) = Nothing
                    Using obj.GetSyncRoot()
                        Dim cmdtext As String = Nothing
                        Try
                            cmdtext = mgr.SQLGenerator.Insert(mgr.MappingEngine, obj, mgr.GetFilterInfo, params, cols)
                        Catch ex As ObjectMappingException When ex.Message.Contains("Cannot save object while it has reference to new object")
                            Return False
                        End Try
                        If cmdtext.Length > 0 Then
                            Dim tran As System.Data.IDbTransaction = mgr.Transaction
                            mgr.BeginTransaction()
                            Try
                                If mgr.SQLGenerator.SupportMultiline Then
                                    Using cmd As System.Data.Common.DbCommand = mgr.CreateDBCommand()
                                        With cmd
                                            .CommandType = System.Data.CommandType.Text
                                            .CommandText = cmdtext
                                            For Each p As System.Data.IDataParameter In params
                                                .Parameters.Add(p)
                                            Next
                                        End With

                                        Dim b As ConnAction = mgr.TestConn(cmd)
                                        Try
                                            mgr.LoadSingleObject(cmd, cols, obj, False, False, True)
                                        Finally
                                            mgr.CloseConn(b)
                                        End Try
                                        'obj.AcceptChanges(cash)
                                    End Using
                                Else
                                    For Each stmt As String In Microsoft.VisualBasic.Split(cmdtext, mgr.SQLGenerator.EndLine)
                                        If stmt = "" Then Continue For
                                        Using cmd As System.Data.Common.DbCommand = mgr.CreateDBCommand()
                                            Dim sel As Boolean = stmt.IndexOf("select") >= 0
                                            With cmd
                                                .CommandType = System.Data.CommandType.Text
                                                .CommandText = stmt
                                                Dim p As IList(Of System.Data.Common.DbParameter) = CType(params, Global.System.Collections.Generic.IList(Of Global.System.Data.Common.DbParameter))
                                                For i As Integer = 0 To ExtractParamsCount(stmt) - 1
                                                    .Parameters.Add(CType(p(0), System.Data.Common.DbParameter))
                                                    p.RemoveAt(0)
                                                Next
                                            End With

                                            Dim b As ConnAction = mgr.TestConn(cmd)
                                            Try
                                                If sel Then
                                                    mgr.LoadSingleObject(cmd, cols, obj, False, False, True)
                                                Else
                                                    cmd.ExecuteNonQuery()
                                                End If
                                            Finally
                                                mgr.CloseConn(b)
                                            End Try
                                        End Using
                                    Next
                                    'obj.AcceptChanges(cash)
                                End If
                            Finally
                                If tran Is Nothing Then
                                    mgr.Commit()
                                End If
                            End Try
                        End If
                    End Using
                    err = False
                Finally
                    If Not err Then
                        obj.SetLoaded(True)
                        'If obj.ObjectState = ObjectState.Modified Then
                        '    obj.SetObjectState(ObjectState.None)
                        'End If
                    End If
                End Try
                Return True
            End Function

            Public Sub M2MSave(ByVal mgr As OrmReadOnlyDBManager, ByVal obj As IKeyEntity, ByVal t As Type, ByVal direct As String, ByVal el As EditableListBase)
                If obj Is Nothing Then
                    Throw New ArgumentNullException("obj")
                End If

                If el Is Nothing Then
                    Throw New ArgumentNullException("el")
                End If

                Dim tt As Type = obj.GetType
                Dim p As New ParamMgr(mgr.SQLGenerator, "p")
                Dim cmd_text As String = mgr.SQLGenerator.SaveM2M(mgr.MappingEngine, obj, mgr.MappingEngine.GetM2MRelationForEdit(tt, t, direct), el, p)

                If Not String.IsNullOrEmpty(cmd_text) Then
                    Dim [error] As Boolean = True
                    Dim tran As System.Data.Common.DbTransaction = mgr.Transaction
                    mgr.BeginTransaction()
                    Try
                        Using cmd As New System.Data.SqlClient.SqlCommand(cmd_text)
                            With cmd
                                .CommandType = System.Data.CommandType.Text
                                p.AppendParams(.Parameters)
                            End With

                            Dim r As ConnAction = mgr.TestConn(cmd)
                            Try
                                Dim i As Integer = cmd.ExecuteNonQuery()
                                [error] = i = 0
                            Finally
                                mgr.CloseConn(r)
                            End Try
                        End Using
                    Finally
                        If tran Is Nothing Then
                            If [error] Then
                                mgr.Rollback()
                            Else
                                mgr.Commit()
                            End If
                        End If
                    End Try
                End If
            End Sub

            Public Sub DeleteObject(ByVal mgr As OrmReadOnlyDBManager, ByVal obj As ICachedEntity)
                mgr.Invariant()

                If obj Is Nothing Then
                    Throw New ArgumentNullException("obj parameter cannot be nothing")
                End If

                Assert(obj.ObjectState = ObjectState.Deleted, "Object " & obj.ObjName & " should be in Deleted state")

                'Dim t As Type = obj.GetType

                Dim params As IEnumerable(Of System.Data.Common.DbParameter) = Nothing
                Using obj.GetSyncRoot()
                    Dim cmdtext As String = mgr.SQLGenerator.Delete(mgr.MappingEngine, obj, params, mgr.GetFilterInfo)
                    If cmdtext.Length > 0 Then
                        Dim [error] As Boolean = True
                        Dim tran As System.Data.Common.DbTransaction = mgr.Transaction
                        mgr.BeginTransaction()
                        Try
                            If mgr.SQLGenerator.SupportMultiline Then
                                Using cmd As System.Data.Common.DbCommand = mgr.CreateDBCommand()
                                    With cmd
                                        .CommandType = System.Data.CommandType.Text
                                        .CommandText = cmdtext
                                        For Each p As System.Data.Common.DbParameter In params
                                            .Parameters.Add(p)
                                        Next
                                    End With

                                    Dim b As ConnAction = mgr.TestConn(cmd)
                                    Try
                                        Dim i As Integer = cmd.ExecuteNonQuery

                                        [error] = i = 0
                                    Finally
                                        mgr.CloseConn(b)
                                    End Try
                                End Using
                            Else
                                For Each stmt As String In Microsoft.VisualBasic.Split(cmdtext, mgr.SQLGenerator.EndLine)
                                    If stmt = "" Then Continue For
                                    Using cmd As System.Data.Common.DbCommand = mgr.CreateDBCommand()
                                        With cmd
                                            .CommandType = System.Data.CommandType.Text
                                            .CommandText = stmt
                                            Dim p As IList(Of System.Data.Common.DbParameter) = Nothing
                                            For j As Integer = 0 To ExtractParamsCount(stmt) - 1
                                                .Parameters.Add(CType(p(0), System.Data.IDataParameter))
                                                p.RemoveAt(0)
                                            Next
                                        End With

                                        Dim b As ConnAction = mgr.TestConn(cmd)
                                        Try
                                            Dim i As Integer = cmd.ExecuteNonQuery()
                                            If Not stmt.StartsWith("set") Then [error] = i = 0
                                        Finally
                                            mgr.CloseConn(b)
                                        End Try
                                    End Using
                                Next
                            End If
                        Finally
                            If tran Is Nothing Then
                                If [error] Then
                                    mgr.Rollback()
                                Else
                                    mgr.Commit()
                                End If
                            End If
                        End Try

                        If [error] Then
                            Debug.Assert(False)
                            Throw mgr.SQLGenerator.PrepareConcurrencyException(mgr.MappingEngine, obj)
                        End If
                    End If
                End Using
            End Sub

            Public Function Delete(ByVal mgr As OrmReadOnlyDBManager, ByVal f As IEntityFilter) As Integer
                Dim t As Type = Nothing
#If DEBUG Then
                For Each fl As cc.EntityFilter In f.GetAllFilters
                    Dim rt As Type = fl.Template.ObjectSource.GetRealType(mgr.MappingEngine)
                    If t Is Nothing Then
                        t = fl.Template.ObjectSource.GetRealType(mgr.MappingEngine)
                    ElseIf t IsNot fl.Template.ObjectSource.GetRealType(mgr.MappingEngine) Then
                        Throw New InvalidOperationException("All filters must have the same type")
                    End If
                Next
#End If
                Using cmd As System.Data.Common.DbCommand = mgr.CreateDBCommand()
                    Dim params As New ParamMgr(mgr.SQLGenerator, "p")
                    With cmd
                        .CommandText = mgr.SQLGenerator.Delete(mgr.MappingEngine, t, f, params)
                        .CommandType = System.Data.CommandType.Text
                        params.AppendParams(.Parameters)
                    End With

                    Dim r As ConnAction = mgr.TestConn(cmd)
                    Try
                        Return cmd.ExecuteNonQuery()
                    Finally
                        mgr.CloseConn(r)
                    End Try
                End Using
            End Function

        End Class

        Private _connStr As String
        Private _tran As System.Data.Common.DbTransaction
        Private _closeConnOnCommit As ConnAction
        Private _conn As System.Data.Common.DbConnection
        Private _exec As TimeSpan
        Private _fetch As TimeSpan
        Friend _batchSaver As ObjectListSaver
        Private Shared _tsStmt As New TraceSource("Worm.Diagnostics.DB.Stmt", SourceLevels.Information)
        Private _timeout As Nullable(Of Integer)

        Protected Shared _LoadMultipleObjectsMI As Reflection.MethodInfo = Nothing
        Protected Shared _LoadMultipleObjectsMI4 As Reflection.MethodInfo = Nothing

        Public Sub New(ByVal cache As CacheBase, ByVal mpe As ObjectMappingEngine, ByVal generator As SQLGenerator, ByVal connectionString As String)
            MyBase.New(cache, mpe)
            StmtGenerator = generator
            _connStr = connectionString
        End Sub

        Protected Sub New(ByVal mpe As ObjectMappingEngine, ByVal generator As SQLGenerator, ByVal connectionString As String)
            MyBase.New(mpe)
            StmtGenerator = generator
            _connStr = connectionString
        End Sub

        Public Shared ReadOnly Property StmtSource() As TraceSource
            Get
                Return _tsStmt
            End Get
        End Property

        Public Function CreateBatchSaver(Of T As {ObjectListSaver, New})(ByRef createdNew As Boolean) As ObjectListSaver
            createdNew = False
            If _batchSaver Is Nothing Then
l1:
                _batchSaver = New T
                _batchSaver.Manager = Me
                createdNew = True
            ElseIf _batchSaver.GetType IsNot GetType(T) Then
                _batchSaver.Dispose()
                GoTo l1
            End If
            Return _batchSaver
        End Function

        Public Overridable ReadOnly Property AlwaysAdd2Cache() As Boolean
            Get
                Return True
            End Get
        End Property

        Public Property CommandTimeout() As Nullable(Of Integer)
            Get
                Return _timeout
            End Get
            Set(ByVal value As Nullable(Of Integer))
                _timeout = value
            End Set
        End Property

        Public ReadOnly Property SQLGenerator() As SQLGenerator
            Get
                Return CType(StmtGenerator, Database.SQLGenerator)
            End Get
        End Property

        Protected Function CreateConn() As System.Data.Common.DbConnection
            Return SQLGenerator.CreateConnection(_connStr)
        End Function

        Protected Overrides Sub Dispose(ByVal disposing As Boolean)
            Try
                If Not _disposed Then
                    If _conn IsNot Nothing Then Rollback()
                End If
            Finally
                MyBase.Dispose(disposing)
            End Try
        End Sub

        Public ReadOnly Property Transaction() As System.Data.Common.DbTransaction
            Get
                Return _tran
            End Get
        End Property

        Public Function BeginTransaction(ByVal mode As System.Data.IsolationLevel) As System.Data.Common.DbTransaction
            If _conn Is Nothing Then
                _conn = CreateConn()
                _closeConnOnCommit = ConnAction.Destroy
                _conn.Open()
                _tran = _conn.BeginTransaction(mode)
            ElseIf _tran Is Nothing Then
                Dim cs As System.Data.ConnectionState = _conn.State
                If cs = System.Data.ConnectionState.Broken OrElse cs = System.Data.ConnectionState.Closed Then
                    _closeConnOnCommit = ConnAction.Close
                    _conn.Open()
                End If
                _tran = _conn.BeginTransaction(mode)
            End If

            Return _tran
        End Function

        Public Function BeginTransaction() As System.Data.Common.DbTransaction
            If _conn Is Nothing Then
                _conn = CreateConn()
                _closeConnOnCommit = ConnAction.Destroy
                _conn.Open()
                _tran = _conn.BeginTransaction()
            ElseIf _tran Is Nothing Then
                Dim cs As System.Data.ConnectionState = _conn.State
                If cs = System.Data.ConnectionState.Broken OrElse cs = System.Data.ConnectionState.Closed Then
                    _closeConnOnCommit = ConnAction.Close
                    _conn.Open()
                End If
                _tran = _conn.BeginTransaction
            End If

            Return _tran
        End Function

        Public Sub Commit()
            Assert(_conn IsNot Nothing, "Commit operation requires connection")

            If _tran IsNot Nothing Then
                _tran.Commit()
                _tran.Dispose()
                _tran = Nothing

                Select Case _closeConnOnCommit
                    Case ConnAction.Close
                        _conn.Close()
                    Case ConnAction.Destroy
                        _conn.Dispose()
                        _conn = Nothing
                End Select
            End If
        End Sub

        ''' <summary>
        ''' Отменяет изменения, выполненные в транзакции
        ''' </summary>
        ''' <remarks>Состояние измененных объектов после отмены изменений на уровне БД не меняется, так что нужно в ручную снова выполнить загрузку их из БД</remarks>
        Public Sub Rollback()
            Assert(_conn IsNot Nothing, "Rollback operation requires connection")

            If _tran IsNot Nothing Then
                _tran.Rollback()
                _tran.Dispose()
                _tran = Nothing

                Select Case _closeConnOnCommit
                    Case ConnAction.Close
                        _conn.Close()
                    Case ConnAction.Destroy
                        _conn.Dispose()
                        _conn = Nothing
                End Select
            End If
        End Sub

        Protected Friend Overrides ReadOnly Property IdentityString() As String
            Get
                Return MyBase.IdentityString & _connStr
            End Get
        End Property

        'Protected Friend Overrides Sub SetSchema(ByVal schema As ObjectMappingEngine)
        '    _stmtHelper = CType(schema, Database.SQLGenerator)
        '    MyBase.SetSchema(schema)
        'End Sub

        Protected Overloads Overrides Function GetCustDelegate(Of T As {New, IKeyEntity})(ByVal relation As M2MRelation, ByVal filter As IFilter, _
            ByVal sort As Sort, ByVal key As String, ByVal id As String) As OrmManager.ICacheItemProvoder(Of T)
            Return New DistinctRelationFilterCustDelegate(Of T)(Me, relation, CType(filter, IFilter), sort, key, id)
        End Function

        Protected Overloads Overrides Function GetCustDelegate(Of T As {New, IKeyEntity})(ByVal aspect As QueryAspect, ByVal join() As Worm.Criteria.Joins.QueryJoin, ByVal filter As IFilter, _
            ByVal sort As Sort, ByVal key As String, ByVal id As String, Optional ByVal cols As List(Of ColumnAttribute) = Nothing) As OrmManager.ICacheItemProvoder(Of T)
            Return New JoinCustDelegate(Of T)(Me, join, filter, sort, key, id, aspect, cols)
        End Function

        Protected Overloads Overrides Function GetCustDelegate(Of T As {New, IKeyEntity})(ByVal filter As IFilter, _
            ByVal sort As Sort, ByVal key As String, ByVal id As String) As OrmManager.ICacheItemProvoder(Of T)
            Return New FilterCustDelegate(Of T)(Me, filter, sort, key, id)
        End Function

        Protected Overloads Overrides Function GetCustDelegate(Of T As {New, IKeyEntity})(ByVal filter As IFilter, _
            ByVal sort As Sort, ByVal key As String, ByVal id As String, ByVal cols() As String) As OrmManager.ICacheItemProvoder(Of T)
            If cols Is Nothing Then
                Throw New ArgumentNullException("cols")
            End If
            Dim l As New List(Of ColumnAttribute)
            Dim has_id As Boolean = False
            For Each c As String In cols
                Dim col As ColumnAttribute = MappingEngine.GetColumnByPropertyAlias(GetType(T), c)
                If col Is Nothing Then
                    Throw New ArgumentException("Invalid column name " & c)
                End If
                If (MappingEngine.GetAttributes(GetType(T), col) And Field2DbRelations.PK) = Field2DbRelations.PK Then
                    has_id = True
                End If
                l.Add(col)
            Next
            If Not has_id Then
                'l.Add(SQLGenerator.GetColumnByFieldName(GetType(T), OrmBaseT.PKName))
                l.Add(MappingEngine.GetPrimaryKeys(GetType(T))(0))
            End If
            Return New FilterCustDelegate(Of T)(Me, CType(filter, IFilter), l, sort, key, id)
        End Function

        'Protected Overrides Function GetCustDelegate4Top(Of T As {New, OrmBase})(ByVal top As Integer, ByVal filter As IOrmFilter, _
        '    ByVal sort As Sort, ByVal key As String, ByVal id As String) As OrmManager.ICustDelegate(Of T)
        '    Return New FilterCustDelegate4Top(Of T)(Me, top, filter, sort, key, id)
        'End Function

        Protected Overloads Overrides Function GetCustDelegate(Of T2 As {New, IKeyEntity})( _
            ByVal obj As _IKeyEntity, ByVal filter As IFilter, ByVal sort As Sort, ByVal queryAscpect() As QueryAspect, _
            ByVal id As String, ByVal key As String, ByVal direct As String) As OrmManager.ICacheItemProvoder(Of T2)
            Return New M2MDataProvider(Of T2)(Me, obj, CType(filter, IFilter), sort, queryAscpect, id, key, direct)
        End Function

        Protected Overloads Overrides Function GetCustDelegate(Of T2 As {New, IKeyEntity})( _
            ByVal obj As _IKeyEntity, ByVal filter As IFilter, ByVal sort As Sort, _
            ByVal id As String, ByVal key As String, ByVal direct As String) As OrmManager.ICacheItemProvoder(Of T2)
            Return New M2MDataProvider(Of T2)(Me, obj, CType(filter, IFilter), sort, New QueryAspect() {}, id, key, direct)
        End Function

        'Protected Overrides Function GetCustDelegateTag(Of T As {New, OrmBase})( _
        '    ByVal obj As T, ByVal filter As IOrmFilter, ByVal sort As String, ByVal sortType As SortType, _
        '    ByVal id As String, ByVal sync As String, ByVal key As String) As OrmManager.ICustDelegate(Of T)
        '    '    Return New M2MCustDelegate(Of T)(Me, obj, filter, sort, sortType, id, sync, key, True)
        '    Throw New NotImplementedException
        'End Function

        Public Function CreateDBCommand() As System.Data.Common.DbCommand
            If _timeout.HasValue Then
                Return SQLGenerator.CreateDBCommand(_timeout.Value)
            Else
                Return SQLGenerator.CreateDBCommand()
            End If
        End Function

        Protected Function FindConnected(ByVal ct As Type, ByVal selectedType As Type, _
            ByVal filterType As Type, ByVal connectedFilter As IFilter, _
            ByVal filter As IFilter, ByVal withLoad As Boolean, _
            ByVal sort As Sort, ByVal q() As QueryAspect) As IList
            Using cmd As System.Data.Common.DbCommand = CreateDBCommand()
                Dim arr As Generic.List(Of ColumnAttribute) = Nothing

                With cmd
                    .CommandType = System.Data.CommandType.Text

                    Dim almgr As AliasMgr = AliasMgr.Create
                    Dim params As New ParamMgr(SQLGenerator, "p")
                    Dim schema2 As IEntitySchema = MappingEngine.GetObjectSchema(selectedType)
                    Dim ctx_schema2 As IContextObjectSchema = TryCast(schema2, IContextObjectSchema)
                    Dim mt_schema2 As IMultiTableObjectSchema = TryCast(schema2, IMultiTableObjectSchema)
                    Dim cs As IEntitySchema = MappingEngine.GetObjectSchema(ct)
                    Dim mms As IConnectedFilter = TryCast(cs, IConnectedFilter)
                    Dim cfi As Object = GetFilterInfo()
                    If mms IsNot Nothing Then
                        cfi = mms.ModifyFilterInfo(cfi, selectedType, filterType)
                    End If
                    'Dim r1 As M2MRelation = Schema.GetM2MRelation(selectedType, filterType)
                    Dim r2 As M2MRelation = MappingEngine.GetM2MRelation(filterType, selectedType, True)
                    Dim id_clm As String = r2.Column

                    Dim sb As New StringBuilder
                    arr = MappingEngine.GetSortedFieldList(ct)
                    If withLoad Then
                        'Dim jc As New OrmFilter(ct, field, selectedType, "ID", FilterOperation.Equal)
                        'Dim j As New OrmJoin(Schema.GetTables(selectedType)(0), JoinType.Join, jc)
                        'Dim js As New List(Of OrmJoin)
                        'js.Add(j)
                        'js.AddRange(Schema.GetAllJoins(selectedType))
                        Dim columns As String = MappingEngine.GetSelectColumnList(selectedType, MappingEngine, Nothing, Nothing, schema2, Nothing)
                        sb.Append(SQLGenerator.Select(MappingEngine, ct, almgr, params, q, arr, columns, cfi))
                    Else
                        sb.Append(SQLGenerator.Select(MappingEngine, ct, almgr, params, q, arr, Nothing, cfi))
                    End If
                    'If withLoad Then
                    '    arr = DatabaseSchema.GetSortedFieldList(ct)
                    '    sb.Append(Schema.Select(ct, almgr, params, arr))
                    'Else
                    '    arr = New Generic.List(Of ColumnAttribute)
                    '    arr.Add(New ColumnAttribute("ID", Field2DbRelations.PK))
                    '    sb.Append(Schema.SelectID(ct, almgr, params))
                    'End If
                    Dim appendMainTable As Boolean = filter IsNot Nothing _
                        OrElse (ctx_schema2 IsNot Nothing AndAlso ctx_schema2.GetContextFilter(GetFilterInfo) IsNot Nothing) _
                        OrElse withLoad OrElse (sort IsNot Nothing AndAlso Not sort.IsExternal) OrElse SQLGenerator.NeedJoin(schema2)
                    'Dim table As String = schema2.GetTables(0)
                    SQLGenerator.AppendNativeTypeJoins(MappingEngine, selectedType, almgr, If(mt_schema2 IsNot Nothing, mt_schema2.GetTables, Nothing), sb, params, cs.Table, id_clm, appendMainTable, GetFilterInfo, schema2)
                    If withLoad AndAlso mt_schema2 IsNot Nothing Then
                        For Each tbl As SourceFragment In mt_schema2.GetTables
                            If almgr.ContainsKey(tbl, Nothing) Then
                                'Dim [alias] As String = almgr.Aliases(tbl)
                                'sb = sb.Replace(tbl.TableName & ".", [alias] & ".")
                                almgr.Replace(MappingEngine, SQLGenerator, tbl, Nothing, sb)
                            End If
                        Next
                    End If
                    Dim con As New Condition.ConditionConstructor
                    con.AddFilter(connectedFilter)
                    con.AddFilter(filter)
                    If ctx_schema2 IsNot Nothing Then
                        con.AddFilter(ctx_schema2.GetContextFilter(GetFilterInfo))
                    End If
                    SQLGenerator.AppendWhere(MappingEngine, ct, con.Condition, almgr, sb, cfi, params)

                    If sort IsNot Nothing AndAlso Not sort.IsExternal Then
                        SQLGenerator.AppendOrder(MappingEngine, sort, almgr, sb, True, Nothing, Nothing)
                    End If

                    params.AppendParams(.Parameters)
                    .CommandText = sb.ToString
                End With

                Dim values As IList = CType(GetType(List(Of )).MakeGenericType(New Type() {ct}).InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, Nothing, Nothing), System.Collections.IList)
                If withLoad Then
                    LoadMultipleObjects(ct, selectedType, cmd, values, Nothing, Nothing)
                Else
                    LoadMultipleObjects(ct, cmd, True, values, arr)
                End If
                Return values
            End Using
        End Function

        'Protected Function FindConnected(ByVal ct As Type, ByVal selectedType As Type, ByVal filterType As Type, _
        '    ByVal connectedFilter As IOrmFilter, ByVal filter As IOrmFilter, ByVal withLoad As Boolean) As IList
        '    Using cmd As System.Data.Common.DbCommand = Schema.CreateDBCommand
        '        Dim arr As Generic.List(Of ColumnAttribute) = Nothing

        '        With cmd
        '            .CommandType = System.Data.CommandType.Text

        '            Dim almgr As AliasMgr = AliasMgr.Create
        '            Dim params As New ParamMgr(Schema, "p")
        '            Dim sb As New StringBuilder
        '            If withLoad Then
        '                arr = DatabaseSchema.GetSortedFieldList(ct)
        '                sb.Append(Schema.Select(ct, almgr, params, arr))
        '            Else
        '                arr = New Generic.List(Of ColumnAttribute)
        '                arr.Add(New ColumnAttribute("ID", Field2DbRelations.PK))
        '                sb.Append(Schema.SelectID(ct, almgr, params))
        '            End If
        '            Dim schema2 As IOrmObjectSchema = Schema.GetObjectSchema(selectedType)
        '            'Dim schema1 As IOrmObjectSchema = Schema.GetObjectSchema(filterType)
        '            'Dim r1 As M2MRelation = Schema.GetM2MRelation(selectedType, filterType)
        '            Dim r2 As M2MRelation = Schema.GetM2MRelation(filterType, selectedType)
        '            Dim id_clm As String = r2.Column
        '            Dim appendMainTable As Boolean = filter IsNot Nothing OrElse schema2.GetFilter(GetFilterInfo) IsNot Nothing
        '            'Dim table As String = schema2.GetTables(0)
        '            Schema.AppendJoins(selectedType, almgr, schema2.GetTables, sb, params, Schema.GetObjectSchema(ct).GetTables(0), id_clm, appendMainTable)

        '            Dim con As New OrmCondition.OrmConditionConstructor
        '            con.AddFilter(connectedFilter)
        '            con.AddFilter(filter)
        '            con.AddFilter(schema2.GetFilter(GetFilterInfo))
        '            Schema.AppendWhere(ct, con.Condition, almgr, sb, GetFilterInfo, params)

        '            params.AppendParams(.Parameters)
        '            .CommandText = sb.ToString
        '        End With

        '        Return LoadMultipleObjects(ct, cmd, withLoad, arr)
        '    End Using
        'End Function

        Protected Sub LoadMultipleObjects(ByVal firstType As Type, _
            ByVal secondType As Type, ByVal cmd As System.Data.Common.DbCommand, ByVal values As IList, _
            ByVal first_cols As List(Of ColumnAttribute), ByVal sec_cols As List(Of ColumnAttribute))

            'Dim values As IList
            'values = CType(GetType(List(Of )).MakeGenericType(New Type() {firstType}).InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, Nothing, Nothing), System.Collections.IList)
            If first_cols Is Nothing Then
                first_cols = MappingEngine.GetSortedFieldList(firstType)
            End If

            If sec_cols Is Nothing Then
                sec_cols = MappingEngine.GetSortedFieldList(secondType)
            End If

            Dim b As ConnAction = TestConn(cmd)
            Dim ec As OrmCache = TryCast(_cache, OrmCache)
            Try
                If ec IsNot Nothing Then
                    ec.BeginTrackDelete(firstType)
                    ec.BeginTrackDelete(secondType)
                End If

                Dim et As New PerfCounter
                Using dr As System.Data.Common.DbDataReader = cmd.ExecuteReader
                    _exec = et.GetTime
                    Dim firstidx As Integer = 0
                    Dim ss() As String = MappingEngine.GetPrimaryKeysName(firstType, MappingEngine, False, Nothing, Nothing, Nothing)
                    If ss.Length > 1 Then
                        Throw New OrmManagerException("Connected type must use single primary key")
                    End If
                    Dim pk_name As String = ss(0)
                    Try
                        firstidx = dr.GetOrdinal(pk_name)
                    Catch ex As IndexOutOfRangeException
                        If _mcSwitch.TraceError Then
                            Trace.WriteLine("Invalid column name " & pk_name & " in " & cmd.CommandText)
                            Trace.WriteLine(Environment.StackTrace)
                        End If
                        Throw New OrmManagerException("Cannot get first primary key ordinal", ex)
                    End Try

                    Dim secidx As Integer = 0
                    ss = MappingEngine.GetPrimaryKeysName(secondType, MappingEngine, False, Nothing, Nothing, Nothing)
                    If ss.Length > 1 Then
                        Throw New OrmManagerException("Connected type must use single primary key")
                    End If
                    Dim pk2_name As String = ss(0)
                    Try
                        secidx = dr.GetOrdinal(pk2_name)
                    Catch ex As IndexOutOfRangeException
                        If _mcSwitch.TraceError Then
                            Trace.WriteLine("Invalid column name " & pk2_name & " in " & cmd.CommandText)
                            Trace.WriteLine(Environment.StackTrace)
                        End If
                        Throw New OrmManagerException("Cannot get second primary key ordinal", ex)
                    End Try

                    Dim dic1 As IDictionary = GetDictionary(firstType)
                    Dim dic2 As IDictionary = GetDictionary(secondType)
                    Dim oschema As IEntitySchema = MappingEngine.GetObjectSchema(firstType)
                    Dim oschema2 As IEntitySchema = MappingEngine.GetObjectSchema(secondType)
                    Dim props As IDictionary = MappingEngine.GetProperties(firstType, oschema)
                    Dim cm As Collections.IndexedCollection(Of String, MapField2Column) = oschema.GetFieldColumnMap

                    Dim props2 As IDictionary = MappingEngine.GetProperties(secondType, oschema2)
                    Dim cm2 As Collections.IndexedCollection(Of String, MapField2Column) = oschema2.GetFieldColumnMap

                    Dim ft As New PerfCounter
                    Do While dr.Read
                        Dim id1 As Object = dr.GetValue(firstidx)
                        Dim obj1 As IKeyEntity = GetOrmBaseFromCacheOrCreate(id1, firstType)
                        If (ec Is Nothing OrElse Not ec.IsDeleted(obj1)) AndAlso obj1.ObjectState <> ObjectState.Modified Then
                            Using obj1.GetSyncRoot()
                                'If obj1.IsLoaded Then obj1.IsLoaded = False
                                Dim lock As IDisposable = Nothing
                                Try
                                    Dim ro As _IEntity = LoadFromDataReader(obj1, dr, first_cols, False, 0, dic1, True, lock, oschema, cm, props)
                                    AfterLoadingProcess(dic1, obj1, lock, ro)
                                    values.Add(ro)
                                Finally
                                    If lock IsNot Nothing Then
                                        lock.Dispose()
                                    End If
                                End Try
                            End Using
                        Else
                            values.Add(obj1)
                        End If

                        Dim id2 As Object = dr.GetValue(secidx)
                        Dim obj2 As IKeyEntity = GetOrmBaseFromCacheOrCreate(id2, secondType)
                        If (ec Is Nothing OrElse Not ec.IsDeleted(obj2)) AndAlso obj2.ObjectState <> ObjectState.Modified Then
                            Using obj2.GetSyncRoot()
                                'If obj2.IsLoaded Then obj2.IsLoaded = False
                                Dim lock As IDisposable = Nothing
                                Try
                                    Dim ro2 As _IEntity = LoadFromDataReader(obj2, dr, sec_cols, False, first_cols.Count, dic2, True, lock, oschema2, cm2, props2)
                                    AfterLoadingProcess(dic2, obj2, lock, ro2)
                                Finally
                                    If lock IsNot Nothing Then
                                        'Threading.Monitor.Exit(dic2)
                                        lock.Dispose()
                                    End If
                                End Try
                            End Using
                        End If
                    Loop
                    _fetch = ft.GetTime

                End Using
            Finally
                If ec IsNot Nothing Then
                    ec.EndTrackDelete(secondType)
                    ec.EndTrackDelete(firstType)
                End If
                CloseConn(b)
            End Try

        End Sub

        Public Sub LoadMultipleObjects(ByVal t As Type, _
            ByVal cmd As System.Data.Common.DbCommand, _
            ByVal withLoad As Boolean, _
            ByVal values As IList, ByVal selectList As Generic.List(Of ColumnAttribute))

            Dim flags As Reflection.BindingFlags = Reflection.BindingFlags.NonPublic Or Reflection.BindingFlags.Instance

            If _LoadMultipleObjectsMI4 Is Nothing Then
                For Each mi2 As Reflection.MethodInfo In Me.GetType.GetMethods(flags)
                    If mi2.Name = "LoadMultipleObjects" AndAlso mi2.IsGenericMethod AndAlso mi2.GetParameters.Length = 4 Then
                        _LoadMultipleObjectsMI4 = mi2
                        Exit For
                    End If
                Next

                If _LoadMultipleObjectsMI4 Is Nothing Then
                    Throw New OrmManagerException("Cannot find method LoadMultipleObjects")
                End If
            End If

            Dim mi_real As Reflection.MethodInfo = _LoadMultipleObjectsMI4.MakeGenericMethod(New Type() {t})

            mi_real.Invoke(Me, flags, Nothing, _
                New Object() {cmd, withLoad, values, selectList}, Nothing)

        End Sub

        Public Sub LoadMultipleObjects(ByVal t As Type, _
            ByVal cmd As System.Data.Common.DbCommand, _
            ByVal withLoad As Boolean, _
            ByVal values As IList, ByVal selectList As Generic.List(Of ColumnAttribute), _
            ByVal oschema As IEntitySchema, _
            ByVal fields_idx As Collections.IndexedCollection(Of String, MapField2Column))
            'Dim ltg As Type = GetType(IList(Of ))
            'Dim lt As Type = ltg.MakeGenericType(New Type() {t})
            Dim flags As Reflection.BindingFlags = Reflection.BindingFlags.Public Or Reflection.BindingFlags.Instance

            If _LoadMultipleObjectsMI Is Nothing Then
                For Each mi2 As Reflection.MethodInfo In Me.GetType.GetMethods(flags)
                    If mi2.Name = "QueryObjects" AndAlso mi2.IsGenericMethod AndAlso mi2.GetParameters.Length = 6 Then
                        _LoadMultipleObjectsMI = mi2
                        Exit For
                    End If
                Next

                If _LoadMultipleObjectsMI Is Nothing Then
                    Throw New OrmManagerException("Cannot find method QueryObjects")
                End If
            End If

            Dim mi_real As Reflection.MethodInfo = _LoadMultipleObjectsMI.MakeGenericMethod(New Type() {t})

            mi_real.Invoke(Me, flags, Nothing, _
                New Object() {cmd, withLoad, values, selectList, oschema, fields_idx}, Nothing)

        End Sub

        'Protected Overrides Function GetDataTableInternal(ByVal t As System.Type, ByVal obj As OrmBase, _
        '    ByVal filter As IOrmFilter, ByVal appendJoins As Boolean, Optional ByVal tag As String = Nothing) As System.Data.DataTable

        '    Throw New NotSupportedException

        '    'If obj Is Nothing Then
        '    '    Throw New ArgumentNullException("obj")
        '    'End If

        '    'Dim mt As Type = obj.GetType
        '    'Dim ct As Type = Schema.GetConnectedType(mt, t)
        '    'If ct IsNot Nothing Then
        '    '    Dim f1 As String = Schema.GetConnectedTypeField(ct, mt)
        '    '    Dim f2 As String = Schema.GetConnectedTypeField(ct, t)
        '    '    Dim fl As New OrmFilter(ct, f1, obj, FilterOperation.Equal)
        '    '    Dim dt As New System.Data.DataTable()
        '    '    dt.TableName = "table1"
        '    '    dt.Locale = System.Globalization.CultureInfo.CurrentCulture
        '    '    Dim col1 As String = mt.Name & "ID"
        '    '    Dim col2 As String = t.Name & "ID"
        '    '    dt.Columns.Add(col1, GetType(Integer))
        '    '    dt.Columns.Add(col2, GetType(Integer))

        '    '    For Each o As OrmBase In FindConnected(ct, t, mt, fl, filter, True)
        '    '        Dim id1 As Integer = CType(Schema.GetFieldValue(o, f1), OrmBase).Identifier
        '    '        Dim id2 As Integer = CType(Schema.GetFieldValue(o, f2), OrmBase).Identifier
        '    '        Dim dr As System.Data.DataRow = dt.NewRow
        '    '        dr(col1) = id1
        '    '        dr(col2) = id2
        '    '        dt.Rows.Add(dr)
        '    '    Next

        '    '    Return dt
        '    'Else
        '    '    Using cmd As System.Data.Common.DbCommand = Schema.CreateDBCommand
        '    '        With cmd
        '    '            .CommandType = System.Data.CommandType.Text
        '    '            Dim params As IList(Of System.Data.Common.DbParameter) = Nothing
        '    '            .CommandText = Schema.SelectM2M(obj, t, filter, GetFilterInfo, appendJoins, params, tag)
        '    '            For Each p As System.Data.Common.DbParameter In params
        '    '                .Parameters.Add(p)
        '    '            Next
        '    '        End With

        '    '        Dim b As ConnAction = TestConn(cmd)
        '    '        Try
        '    '            Using da As System.Data.Common.DbDataAdapter = Schema.CreateDataAdapter()

        '    '                da.SelectCommand = cmd
        '    '                da.TableMappings.Add(CType(Schema.GetJoinSelectMapping(mt, t), ICloneable).Clone)
        '    '                Dim dt As New System.Data.DataTable()
        '    '                dt.TableName = "table1"
        '    '                dt.Locale = System.Globalization.CultureInfo.CurrentCulture
        '    '                da.Fill(dt)
        '    '                Return dt
        '    '            End Using
        '    '        Finally
        '    '            CloseConn(b)
        '    '        End Try
        '    '    End Using
        '    'End If

        'End Function

        Protected Overrides Function GetObjects(Of T As {IKeyEntity, New})(ByVal type As Type, ByVal ids As Generic.IList(Of Object), ByVal f As IFilter, _
            ByVal relation As M2MRelation, ByVal idsSorted As Boolean, ByVal withLoad As Boolean) As IDictionary(Of Object, EditableList)
            Invariant()

            If ids Is Nothing Then
                Throw New ArgumentNullException("ids")
            End If

            If ids.Count < 1 Then
                Return Nothing
            End If

            Dim almgr As AliasMgr = AliasMgr.Create
            Dim params As New ParamMgr(SQLGenerator, "p")
            'Dim arr As Generic.List(Of ColumnAttribute) = Nothing
            Dim sb As New StringBuilder
            Dim type2load As Type = GetType(T)
            Dim ct As Type = MappingEngine.GetConnectedType(type, type2load)
            Dim direct As String = relation.Key

            'Dim dt As New System.Data.DataTable()
            'dt.TableName = "table1"
            'dt.Locale = System.Globalization.CultureInfo.CurrentCulture

            Dim edic As New Dictionary(Of Object, EditableList)

            If ct IsNot Nothing Then
                'If Not direct Then
                '    Throw New NotSupportedException("Tag is not supported with connected type")
                'End If

                'Dim oschema2 As IOrmObjectSchema = DbSchema.GetObjectSchema(type2load)
                'Dim r2 As M2MRelation = DbSchema.GetM2MRelation(type2load, type, direct)
                Dim f1 As String = MappingEngine.GetConnectedTypeField(ct, type, M2MRelation.GetRevKey(direct))
                Dim f2 As String = MappingEngine.GetConnectedTypeField(ct, type2load, direct)
                'Dim col1 As String = type.Name & "ID"
                'Dim col2 As String = orig_type.Name & "ID"
                'dt.Columns.Add(col1, GetType(Integer))
                'dt.Columns.Add(col2, GetType(Integer))
                Dim oschema As IEntitySchema = MappingEngine.GetObjectSchema(ct)

                For Each o As IKeyEntity In GetObjects(ct, ids, f, withLoad, f1, idsSorted)
                    'Dim o1 As OrmBase = CType(DbSchema.GetFieldValue(o, f1), OrmBase)
                    'Dim o2 As OrmBase = CType(DbSchema.GetFieldValue(o, f2), OrmBase)
                    Dim o1 As IKeyEntity = CType(o.GetValueOptimized(Nothing, f1, oschema), IKeyEntity)
                    Dim o2 As IKeyEntity = CType(o.GetValueOptimized(Nothing, f2, oschema), IKeyEntity)

                    Dim id1 As Object = o1.Identifier
                    Dim id2 As Object = o2.Identifier
                    'Dim k As Integer = o1.Identifier
                    'Dim v As Integer = o2.Identifier
                    Dim toAdd As IKeyEntity = o1
                    If o2.GetType Is type Then
                        id1 = o2.Identifier
                        id2 = o1.Identifier
                        toAdd = o2
                    End If

                    Dim el As EditableList = Nothing
                    If edic.TryGetValue(id1, el) Then
                        el.Add(toAdd)
                    Else
                        Dim l As New List(Of Object)
                        l.Add(id2)
                        el = New EditableList(id1, l, type, type2load, Nothing)
                        edic.Add(id1, el)
                    End If
                Next
            Else
                Dim oschema2 As IEntitySchema = MappingEngine.GetObjectSchema(type2load)
                Dim ctx_oschema2 As IContextObjectSchema = TryCast(oschema2, IContextObjectSchema)
                Dim r2 As M2MRelation = MappingEngine.GetM2MRelation(type2load, type, direct)
                Dim appendMainTable As Boolean = f IsNot Nothing OrElse _
                    (ctx_oschema2 IsNot Nothing AndAlso ctx_oschema2.GetContextFilter(GetFilterInfo) IsNot Nothing)
                sb.Append(SQLGenerator.SelectM2M(MappingEngine, type2load, type, New QueryAspect() {}, appendMainTable, True, GetFilterInfo, params, almgr, withLoad, direct))

                If Not SQLGenerator.AppendWhere(MappingEngine, type2load, CType(f, IFilter), almgr, sb, GetFilterInfo, params) Then
                    sb.Append(" where 1=1 ")
                End If
                Dim dic As IDictionary = CType(GetDictionary(Of T)(), System.Collections.IDictionary)

                Dim pcnt As Integer = params.Params.Count
                Dim nidx As Integer = pcnt
                For Each cmd_str As Pair(Of String, Integer) In GetFilters(CType(ids, List(Of Object)), relation.Table, r2.Column, almgr, params, idsSorted)
                    Dim sb_cmd As New StringBuilder
                    sb_cmd.Append(sb.ToString).Append(cmd_str.First)
                    'Dim msort As Boolean = False
                    'If Not String.IsNullOrEmpty(sort) AndAlso Schema.GetObjectSchema(original_type).IsExternalSort(sort) Then
                    '    msort = True
                    'Else
                    '    Schema.AppendOrder(original_type, sort, sortType, almgr, sb_cmd)
                    'End If

                    Using cmd As System.Data.Common.DbCommand = CreateDBCommand()
                        With cmd
                            .CommandType = System.Data.CommandType.Text
                            .CommandText = sb_cmd.ToString
                            params.AppendParams(.Parameters, 0, pcnt)
                            params.AppendParams(.Parameters, nidx, cmd_str.Second - nidx)
                            nidx = cmd_str.Second
                        End With
                        Dim arr As Generic.IList(Of ColumnAttribute) = Nothing
                        If withLoad Then
                            arr = MappingEngine.GetSortedFieldList(type2load)
                        End If
                        Dim ec As OrmCache = TryCast(_cache, OrmCache)

                        Dim b As ConnAction = TestConn(cmd)
                        Try
                            If withLoad AndAlso ec IsNot Nothing Then
                                ec.BeginTrackDelete(type2load)
                            End If
                            Dim et As New PerfCounter
                            Using dr As System.Data.Common.DbDataReader = cmd.ExecuteReader
                                _exec = et.GetTime

                                Dim props As IDictionary = MappingEngine.GetProperties(type2load, oschema2)
                                Dim cm As Collections.IndexedCollection(Of String, MapField2Column) = oschema2.GetFieldColumnMap

                                Dim ft As New PerfCounter
                                Do While dr.Read
                                    Dim id1 As Object = dr.GetValue(0)
                                    Dim id2 As Object = dr.GetValue(1)
                                    Dim obj As T = GetOrmBaseFromCacheOrCreate(Of T)(id2)
                                    Dim el As EditableList = Nothing
                                    If edic.TryGetValue(id1, el) Then
                                        el.Add(obj)
                                    Else
                                        Dim l As New List(Of Object)
                                        l.Add(id2)
                                        el = New EditableList(id1, l, type, type2load, Nothing)
                                        edic.Add(id1, el)
                                    End If
                                    If withLoad AndAlso (ec Is Nothing OrElse Not ec.IsDeleted(type2load, obj.Key)) Then
                                        If obj.ObjectState <> ObjectState.Modified Then
                                            Using obj.GetSyncRoot()
                                                'If obj.IsLoaded Then obj.IsLoaded = False
                                                Dim lock As IDisposable = Nothing
                                                Try
                                                    Dim ro As _IEntity = LoadFromDataReader(obj, dr, arr, False, 2, dic, True, lock, oschema2, cm, props)
                                                    AfterLoadingProcess(dic, obj, lock, ro)
                                                Finally
                                                    If lock IsNot Nothing Then
                                                        'Threading.Monitor.Exit(dic)
                                                        lock.Dispose()
                                                    End If
                                                End Try
                                            End Using
                                        End If
                                    End If
                                Loop
                                _fetch = ft.GetTime
                            End Using
                        Finally
                            If withLoad AndAlso ec IsNot Nothing Then
                                ec.EndTrackDelete(type2load)
                            End If
                            CloseConn(b)
                        End Try
                    End Using
                Next
            End If
            Return edic
        End Function

        Protected Overloads Function GetObjects(ByVal ct As Type, ByVal ids As Generic.IList(Of Object), _
            ByVal f As IFilter, ByVal withLoad As Boolean, ByVal fieldName As String, ByVal idsSorted As Boolean) As IList
            If ids Is Nothing Then
                Throw New ArgumentNullException("ids")
            End If

            Dim values As IList = CType(GetType(List(Of )).MakeGenericType(New Type() {ct}).InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, Nothing, Nothing), System.Collections.IList)
            If ids.Count < 1 Then
                Return values
            End If

            Dim original_type As Type = ct
            Dim almgr As AliasMgr = AliasMgr.Create
            Dim params As New ParamMgr(SQLGenerator, "p")
            Dim arr As Generic.List(Of ColumnAttribute) = Nothing
            Dim sb As New StringBuilder
            If withLoad Then
                arr = MappingEngine.GetSortedFieldList(original_type)
                sb.Append(SQLGenerator.Select(MappingEngine, original_type, almgr, params, arr, Nothing, GetFilterInfo))
            Else
                arr = New Generic.List(Of ColumnAttribute)
                'arr.Add(New ColumnAttribute(OrmBaseT.PKName, Field2DbRelations.PK))
                arr.Add(MappingEngine.GetPrimaryKeys(original_type)(0))
                sb.Append(SQLGenerator.SelectID(MappingEngine, original_type, almgr, params, GetFilterInfo))
            End If

            If Not SQLGenerator.AppendWhere(MappingEngine, original_type, CType(f, IFilter), almgr, sb, GetFilterInfo, params) Then
                sb.Append(" where 1=1 ")
            End If

            Dim pcnt As Integer = params.Params.Count
            Dim nidx As Integer = pcnt
            For Each cmd_str As Pair(Of String, Integer) In GetFilters(CType(ids, List(Of Object)), fieldName, almgr, params, original_type, idsSorted)
                Dim sb_cmd As New StringBuilder
                sb_cmd.Append(sb.ToString).Append(cmd_str.First)
                'Dim msort As Boolean = False
                'If Not String.IsNullOrEmpty(sort) AndAlso Schema.GetObjectSchema(original_type).IsExternalSort(sort) Then
                '    msort = True
                'Else
                '    Schema.AppendOrder(original_type, sort, sortType, almgr, sb_cmd)
                'End If

                Using cmd As System.Data.Common.DbCommand = CreateDBCommand()
                    With cmd
                        .CommandType = System.Data.CommandType.Text
                        .CommandText = sb_cmd.ToString
                        params.AppendParams(.Parameters, 0, pcnt)
                        params.AppendParams(.Parameters, nidx, cmd_str.Second - nidx)
                        nidx = cmd_str.Second
                    End With
                    LoadMultipleObjects(original_type, cmd, withLoad, values, arr)
                    'If msort Then
                    '    objs = Schema.GetObjectSchema(original_type).ExternalSort(sort, sortType, objs)
                    'End If
                End Using

                'params.Clear(pcnt)
            Next
            Return values
        End Function

        Protected Overrides Function GetObjects(Of T As {IKeyEntity, New})(ByVal ids As Generic.IList(Of Object), ByVal f As IFilter, ByVal objs As List(Of T), _
            ByVal withLoad As Boolean, ByVal propertyAlias As String, ByVal idsSorted As Boolean) As Generic.IList(Of T)
            Invariant()

            If ids Is Nothing Then
                Throw New ArgumentNullException("ids")
            End If

            If ids.Count < 1 Then
                Return objs
            End If

            Dim original_type As Type = GetType(T)
            Dim almgr As AliasMgr = AliasMgr.Create
            Dim params As New ParamMgr(SQLGenerator, "p")
            Dim arr As Generic.List(Of ColumnAttribute) = Nothing
            Dim sb As New StringBuilder
            If withLoad Then
                arr = MappingEngine.GetSortedFieldList(original_type)
                sb.Append(SQLGenerator.Select(MappingEngine, original_type, almgr, params, arr, Nothing, GetFilterInfo))
            Else
                arr = New Generic.List(Of ColumnAttribute)
                'arr.Add(New ColumnAttribute(OrmBaseT.PKName, Field2DbRelations.PK))
                arr.Add(MappingEngine.GetPrimaryKeys(original_type)(0))
                sb.Append(SQLGenerator.SelectID(MappingEngine, original_type, almgr, params, GetFilterInfo))
            End If

            If Not SQLGenerator.AppendWhere(MappingEngine, original_type, CType(f, IFilter), almgr, sb, GetFilterInfo, params) Then
                sb.Append(" where 1=1 ")
            End If

            Dim pcnt As Integer = params.Params.Count
            Dim nidx As Integer = pcnt
            For Each cmd_str As Pair(Of String, Integer) In GetFilters(CType(ids, List(Of Object)), propertyAlias, almgr, params, original_type, idsSorted)
                Dim sb_cmd As New StringBuilder
                sb_cmd.Append(sb.ToString).Append(cmd_str.First)
                'Dim msort As Boolean = False
                'If Not String.IsNullOrEmpty(sort) AndAlso Schema.GetObjectSchema(original_type).IsExternalSort(sort) Then
                '    msort = True
                'Else
                '    Schema.AppendOrder(original_type, sort, sortType, almgr, sb_cmd)
                'End If

                Using cmd As System.Data.Common.DbCommand = CreateDBCommand()
                    With cmd
                        .CommandType = System.Data.CommandType.Text
                        .CommandText = sb_cmd.ToString
                        params.AppendParams(.Parameters, 0, pcnt)
                        params.AppendParams(.Parameters, nidx, cmd_str.Second - nidx)
                        nidx = cmd_str.Second
                    End With
                    LoadMultipleObjects(Of T)(cmd, withLoad, objs, arr)
                    'If msort Then
                    '    objs = Schema.GetObjectSchema(original_type).ExternalSort(sort, sortType, objs)
                    'End If
                End Using

                'params.Clear(pcnt)
            Next
            Return objs
        End Function

        Protected Friend Overrides Function GetStaticKey() As String
            Return String.Empty
        End Function

        Public Function ExecuteReader(ByVal cmd As System.Data.Common.DbCommand) As System.Data.Common.DbDataReader
            Dim b As ConnAction = TestConn(cmd)
            Try
                Return cmd.ExecuteReader
            Finally
                CloseConn(b)
            End Try
        End Function

        Public Function ExecuteScalar(ByVal cmd As System.Data.Common.DbCommand) As Object
            Dim b As ConnAction = TestConn(cmd)
            Try
                Return cmd.ExecuteScalar
            Finally
                CloseConn(b)
            End Try
        End Function

        Public Function ExecuteNonQuery(ByVal cmd As System.Data.Common.DbCommand) As Integer
            Dim b As ConnAction = TestConn(cmd)
            Try
                Return cmd.ExecuteNonQuery
            Finally
                CloseConn(b)
            End Try
        End Function

        'Protected Overridable Function GetNewObject(ByVal type As Type, ByVal id As Integer) As OrmBase
        '    Dim o As OrmBase = Nothing
        '    If  IsNot Nothing Then
        '        o = FindNewDelegate(type, id)
        '    End If
        '    Return o
        'End Function

        Protected Friend Overrides Sub LoadObject(ByVal obj As _ICachedEntity)
            Invariant()

            If obj Is Nothing Then
                Throw New ArgumentNullException("obj")
            End If

            Dim original_type As Type = obj.GetType

            'Dim filter As New cc.EntityFilter(original_type, "ID", _
            '    New EntityValue(obj), Worm.Criteria.FilterOperation.Equal)
            Dim c As New Condition.ConditionConstructor '= Database.Criteria.Conditions.Condition.ConditionConstructor
            For Each p As PKDesc In obj.GetPKValues
                c.AddFilter(New cc.EntityFilter(original_type, p.PropertyAlias, New ScalarValue(p.Value), Worm.Criteria.FilterOperation.Equal))
            Next
            Dim filter As IFilter = c.Condition

            Using cmd As System.Data.Common.DbCommand = CreateDBCommand()
                Dim arr As Generic.List(Of ColumnAttribute) = MappingEngine.GetSortedFieldList(original_type)

                With cmd
                    .CommandType = System.Data.CommandType.Text

                    Dim almgr As AliasMgr = AliasMgr.Create
                    Dim params As New ParamMgr(SQLGenerator, "p")
                    Dim sb As New StringBuilder
                    sb.Append(SQLGenerator.Select(MappingEngine, original_type, almgr, params, arr, Nothing, GetFilterInfo))
                    SQLGenerator.AppendWhere(MappingEngine, original_type, filter, almgr, sb, GetFilterInfo, params)

                    params.AppendParams(.Parameters)
                    .CommandText = sb.ToString
                End With

                'Dim olds As ObjectState = obj.ObjectState
                Dim b As ConnAction = TestConn(cmd)
                Try
                    'Dim newObj As Boolean = obj.ObjectState = ObjectState.Created
                    Dim sync_key As String = "LoadSngObj" & obj.Key & original_type.ToString
                    'Using obj.GetSyncRoot()
                    Using SyncHelper.AcquireDynamicLock(sync_key)
                        'Using obj.GetSyncRoot()
                        LoadSingleObject(cmd, arr, obj, True, True, False)
                        'If newObj AndAlso obj.ObjectState = ObjectState.None OrElse obj.ObjectState = ObjectState.NotLoaded Then
                        '    _cache.UnregisterModification(obj)
                        '    NormalizeObject(obj, GetDictionary(obj.GetType))
                        'End If
                        If obj.ObjectState <> ObjectState.NotFoundInSource Then
                            Add2Cache(obj)
                            obj.AcceptChanges(True, True)
                        End If
                    End Using
                Finally
                    CloseConn(b)
                End Try
                'If olds = obj.ObjectState OrElse obj.ObjectState = ObjectState.Created Then obj.ObjectState = ObjectState.None
                '(olds = ObjectState.Created AndAlso obj.ObjectState = ObjectState.Modified) OrElse
                'If olds = obj.ObjectState Then
                '    obj.ObjectState = ObjectState.None
                'End If
            End Using
        End Sub

        Protected Sub LoadSingleObject(ByVal cmd As System.Data.Common.DbCommand, _
           ByVal arr As Generic.IList(Of ColumnAttribute), ByVal obj As _ICachedEntity, _
           ByVal check_pk As Boolean, ByVal load As Boolean, ByVal modifiedloaded As Boolean)
            Invariant()

            Dim dic As IDictionary = GetDictionary(obj.GetType)

            LoadSingleObject(cmd, arr, obj, check_pk, load, modifiedloaded, dic)
        End Sub

        Protected Sub LoadSingleObject(ByVal cmd As System.Data.Common.DbCommand, _
            ByVal arr As Generic.IList(Of ColumnAttribute), ByVal obj As _ICachedEntity, _
            ByVal check_pk As Boolean, ByVal load As Boolean, ByVal modifiedloaded As Boolean, _
            ByVal dic As IDictionary)
            Invariant()

            Dim ec As OrmCache = TryCast(_cache, OrmCache)
            Try
                If load AndAlso ec IsNot Nothing Then
                    ec.BeginTrackDelete(obj.GetType)
                End If
                Dim et As New PerfCounter
                Using dr As System.Data.Common.DbDataReader = cmd.ExecuteReader
                    Dim k As String = String.Empty
                    If obj.IsPKLoaded Then
                        k = obj.Key.ToString
                    End If
                    Dim sync_key As String = "LoadSngObj" & k & obj.GetType.ToString
                    'Using obj.GetSyncRoot()
                    Using SyncHelper.AcquireDynamicLock(sync_key)
                        'Dim old As Boolean = obj.IsLoaded
                        'If Not modifiedloaded Then obj.IsLoaded = False
                        'obj.IsLoaded = False
                        Dim loaded As Boolean = False
                        Dim oschema As IEntitySchema = MappingEngine.GetObjectSchema(obj.GetType)
                        Dim props As IDictionary = MappingEngine.GetProperties(obj.GetType, oschema)
                        Dim cm As Collections.IndexedCollection(Of String, MapField2Column) = oschema.GetFieldColumnMap
                        'obj = NormalizeObject(obj, dic)
                        Do While dr.Read
                            If loaded Then
                                Throw New OrmManagerException(String.Format("Statement [{0}] returns more than one record", cmd.CommandText))
                            End If
                            If obj.ObjectState <> ObjectState.Deleted AndAlso (Not load OrElse ec Is Nothing OrElse Not ec.IsDeleted(obj)) Then
                                Dim lock As IDisposable = Nothing
                                Try
                                    LoadFromDataReader(obj, dr, arr, check_pk, 0, dic, False, lock, oschema, cm, props)
                                    obj.CorrectStateAfterLoading(False)
                                Finally
                                    If lock IsNot Nothing Then
                                        lock.Dispose()
                                    End If
                                End Try
                                'If lock Then
                                '    Dim co As _ICachedEntity = TryCast(obj, _ICachedEntity)
                                '    If co IsNot Nothing Then
                                '        _cache.UnregisterModification(co)

                                '        If lock Then
                                '            Threading.Monitor.Exit(dic)
                                '            lock = False
                                '        End If
                                '    End If
                                'End If
                                'obj = o
                            Else
                                Exit Do
                            End If
                            loaded = True
                        Loop
                        If dr.RecordsAffected = 0 Then
                            Throw SQLGenerator.PrepareConcurrencyException(MappingEngine, obj)
                        End If

                        If Not obj.IsLoaded AndAlso loaded Then
                            If load Then
                                'Throw New ApplicationException
                                _cache.UnregisterModification(obj)
                                obj.SetObjectState(ObjectState.NotFoundInSource)
                                RemoveObjectFromCache(obj)
                            End If
                        ElseIf Not obj.IsLoaded AndAlso Not loaded AndAlso dr.RecordsAffected > 0 Then
                            'insert without select
                            obj.CreateCopyForSaveNewEntry(Me, Nothing)
                        Else
                            If dr.RecordsAffected <> -1 Then
                                'obj.CreateCopyForSaveNewEntry(Nothing)
                                'obj.ObjectState = ObjectState.None
                                'Throw New ApplicationException
                                'obj.BeginLoading()
                                'obj.Identifier = obj.Identifier
                                'obj.EndLoading()
                            Else
                                If Not obj.IsLoaded Then
                                    'loading non-existent object
                                    _cache.UnregisterModification(obj)
                                    obj.SetObjectState(ObjectState.NotFoundInSource)
                                    RemoveObjectFromCache(obj)
                                End If
                            End If
                        End If
                    End Using
                End Using
                _cache.LogLoadTime(obj, et.GetTime)
            Finally
                If load AndAlso ec IsNot Nothing Then
                    ec.EndTrackDelete(obj.GetType)
                End If
                'CloseConn(b)
            End Try

        End Sub

        Public Function GetSimpleValues(Of T)(ByVal cmd As System.Data.Common.DbCommand) As IList(Of T)
            Dim l As New List(Of T)
            Dim b As ConnAction = TestConn(cmd)
            Try
                Dim n As Boolean = GetType(T).FullName.StartsWith("System.Nullable")
                Using dr As System.Data.IDataReader = cmd.ExecuteReader
                    Do While dr.Read
                        'l.Add(CType(Convert.ChangeType(dr.GetValue(0), GetType(T)), T))
                        If dr.IsDBNull(0) Then
                            l.Add(Nothing)
                        Else
                            If n Then
                                Dim rt As Type = GetType(T).GetGenericArguments(0)
                                Dim o As Object = Convert.ChangeType(dr.GetValue(0), rt)
                                l.Add(CType(o, T))
                            Else
                                l.Add(CType(dr.GetValue(0), T))
                            End If
                        End If

                    Loop
                    Return l
                End Using
            Finally
                CloseConn(b)
            End Try
        End Function

        Public Function GetSimpleValues(ByVal cmd As System.Data.Common.DbCommand, ByVal t As Type) As IList
            Dim l As New ArrayList
            Dim b As ConnAction = TestConn(cmd)
            Try
                Dim n As Boolean = t.FullName.StartsWith("System.Nullable")
                Using dr As System.Data.IDataReader = cmd.ExecuteReader
                    Do While dr.Read
                        'l.Add(CType(Convert.ChangeType(dr.GetValue(0), GetType(T)), T))
                        If dr.IsDBNull(0) Then
                            l.Add(Nothing)
                        Else
                            If n Then
                                Dim rt As Type = t.GetGenericArguments(0)
                                Dim o As Object = Convert.ChangeType(dr.GetValue(0), rt)
                                l.Add(o)
                            Else
                                l.Add(dr.GetValue(0))
                            End If
                        End If

                    Loop
                    Return l
                End Using
            Finally
                CloseConn(b)
            End Try
        End Function

        Protected Friend Sub LoadMultipleObjects(Of T As {_IEntity, New})( _
            ByVal cmd As System.Data.Common.DbCommand, _
            ByVal withLoad As Boolean, ByVal values As IList, _
            ByVal selectList As Generic.List(Of ColumnAttribute))

            Dim oschema As IEntitySchema = MappingEngine.GetObjectSchema(GetType(T))
            Dim fields_idx As Collections.IndexedCollection(Of String, MapField2Column) = oschema.GetFieldColumnMap

            QueryObjects(Of T)(cmd, withLoad, values, selectList, oschema, fields_idx)
        End Sub

        Public Sub QueryMultiTypeObjects( _
            ByVal cmd As System.Data.Common.DbCommand, _
            ByVal values As List(Of ReadOnlyCollection(Of _IEntity)), _
            ByVal types As IDictionary(Of ObjectSource, IEntitySchema), _
            ByVal pdic As Dictionary(Of Type, IDictionary), _
            ByVal selectList As IList(Of SelectExpression))

            Dim c As OrmCache = TryCast(_cache, OrmCache)
            Dim b As ConnAction = TestConn(cmd)
            Try
                _loadedInLastFetch = 0
                If c IsNot Nothing Then
                    For Each os As ObjectSource In types.Keys
                        Dim original_type As Type = os.GetRealType(MappingEngine)
                        c.BeginTrackDelete(original_type)
                    Next
                End If

                Dim objDic As New Dictionary(Of ObjectSource, IDictionary)
                For Each k As KeyValuePair(Of ObjectSource, IEntitySchema) In types
                    objDic.Add(k.Key, GetDictionary(k.Key.GetRealType(MappingEngine), k.Value))
                Next

                Dim et As New PerfCounter
                Using dr As System.Data.Common.DbDataReader = cmd.ExecuteReader
                    _exec = et.GetTime

                    Dim ft As New PerfCounter
                    Do While dr.Read
                        LoadMultiFromResultSet(values, selectList, dr, types, pdic, objDic, _loadedInLastFetch)
                    Loop
                    _fetch = ft.GetTime
                End Using
            Finally
                If c IsNot Nothing Then
                    For Each os As ObjectSource In types.Keys
                        Dim original_type As Type = os.GetRealType(MappingEngine)
                        c.EndTrackDelete(original_type)
                    Next
                End If
                CloseConn(b)
            End Try
        End Sub

        Protected Sub LoadMultiFromResultSet(ByVal values As List(Of ReadOnlyCollection(Of _IEntity)), _
            ByVal selectList As IList(Of SelectExpression), _
            ByVal dr As System.Data.Common.DbDataReader, _
            ByVal types As IDictionary(Of ObjectSource, IEntitySchema), _
            ByVal pdic As Dictionary(Of Type, IDictionary), _
            ByVal objDic As Dictionary(Of ObjectSource, IDictionary), _
            ByRef loaded As Integer)

            Dim odic As New Specialized.OrderedDictionary '(Of ObjectSource, _IEntity)
            Dim dfac As New Dictionary(Of ObjectSource, List(Of Pair(Of String, Object)))
            Dim pkdic As New Dictionary(Of ObjectSource, Integer)
            Dim l As New List(Of _IEntity)

            For i As Integer = 0 To selectList.Count - 1
                Dim se As SelectExpression = selectList(i)
                Dim t As Type = se.ObjectSource.GetRealType(MappingEngine)
                Dim oschema As IEntitySchema = types(se.ObjectSource)

                Dim pi As Reflection.PropertyInfo = Nothing
                Dim c As ColumnAttribute = Nothing
                For Each de As DictionaryEntry In pdic(t)
                    c = CType(de.Key, ColumnAttribute)
                    If c.PropertyAlias = se.PropertyAlias Then
                        pi = CType(de.Value, Reflection.PropertyInfo)
                        Exit For
                    End If
                Next

                Dim att As Field2DbRelations = MappingEngine.GetAttributes(oschema, c)

                If (att And Field2DbRelations.PK) = Field2DbRelations.PK Then
                    Dim obj As _IEntity = CType(odic(se.ObjectSource), _IEntity)
                    If obj Is Nothing Then
                        obj = CType(Activator.CreateInstance(t), _IEntity)
                        obj.SetMgrString(IdentityString)
                        odic.Add(se.ObjectSource, obj)
                    End If

                    Dim ce As _ICachedEntity = TryCast(obj, _ICachedEntity)

                    Dim fv As IDBValueFilter = TryCast(oschema, IDBValueFilter)
                    Dim value As Object = dr.GetValue(i)
                    If fv IsNot Nothing Then
                        value = fv.CreateValue(se.PropertyAlias, obj, value)
                    End If

                    obj.BeginLoading()
                    ParseValueFromDb(dr, att, i, obj, pi, se.PropertyAlias, oschema, value, ce, _
                                     False, Nothing, c)

                    obj.EndLoading()

                    Dim cnt As Integer
                    pkdic.TryGetValue(se.ObjectSource, cnt)
                    pkdic(se.ObjectSource) = cnt + 1
                End If
            Next

            For Each de As DictionaryEntry In odic
                Dim obj As _IEntity = CType(de.Value, _IEntity)
                Dim os As ObjectSource = CType(de.Key, ObjectSource)
                Dim pk_count As Integer
                pkdic.TryGetValue(os, pk_count)
                Dim ce As _ICachedEntity = TryCast(obj, _ICachedEntity)

                If pk_count > 0 Then
                    If ce IsNot Nothing Then
                        ce.PKLoaded(pk_count)

                        Dim c As OrmCache = TryCast(_cache, OrmCache)
                        If c IsNot Nothing AndAlso c.IsDeleted(ce) Then
                            l.Add(obj)
                        End If

                        'Threading.Monitor.Enter(dic)
                        'lock = True
                        Dim fromRS As Boolean = True
                        Dim dic As IDictionary = Nothing
                        objDic.TryGetValue(os, dic)
                        If dic IsNot Nothing Then
                            Dim robj As ICachedEntity = NormalizeObject(ce, dic, fromRS)
                            Dim fromCache As Boolean = Not Object.ReferenceEquals(robj, ce)

                            obj = robj
                            ce = CType(obj, _ICachedEntity)
                            'SyncLock dic
                            If fromCache Then
                                If obj.ObjectState = ObjectState.Created Then
                                    'Using obj.GetSyncRoot
                                    l.Add(obj)
                                    'End Using
                                ElseIf obj.ObjectState = ObjectState.Modified OrElse obj.ObjectState = ObjectState.Deleted Then
                                    l.Add(obj)
                                    'Else
                                    '    obj.BeginLoading()
                                End If
                                'Else
                                '    If fromRS Then
                                '        'Else
                                '        '    If obj.ObjectState = ObjectState.Created Then
                                '        '        ce.CreateCopyForSaveNewEntry(Me, oldpk)
                                '        '        'Cache.Modified(obj).Reason = ModifiedObject.ReasonEnum.SaveNew
                                '        '    End If
                                '    End If
                            End If
                            'End SyncLock
                        End If
                    End If
                    'ElseIf ce IsNot Nothing AndAlso Not fromRS AndAlso obj.ObjectState = ObjectState.Created Then
                    '    ce.CreateCopyForSaveNewEntry(Me, Nothing)
                End If
            Next

            If l.Count = odic.Count Then
                values.Add(New ReadOnlyObjectList(Of _IEntity)(l))
                Return
            End If

            For i As Integer = 0 To selectList.Count - 1
                Dim se As SelectExpression = selectList(i)
                Dim t As Type = se.ObjectSource.GetRealType(MappingEngine)
                Dim oschema As IEntitySchema = types(se.ObjectSource)

                Dim pi As Reflection.PropertyInfo = Nothing
                Dim c As ColumnAttribute = Nothing
                For Each de As DictionaryEntry In pdic(t)
                    c = CType(de.Key, ColumnAttribute)
                    If c.PropertyAlias = se.PropertyAlias Then
                        pi = CType(de.Value, Reflection.PropertyInfo)
                        Exit For
                    End If
                Next

                Dim att As Field2DbRelations = MappingEngine.GetAttributes(oschema, c)
                If (att And Field2DbRelations.PK) <> Field2DbRelations.PK Then
                    Dim obj As _IEntity = CType(odic(se.ObjectSource), _IEntity)
                    Dim fac As List(Of Pair(Of String, Object)) = Nothing
                    If Not dfac.TryGetValue(se.ObjectSource, fac) Then
                        fac = New List(Of Pair(Of String, Object))
                        dfac.Add(se.ObjectSource, fac)
                    End If

                    Dim ce As _ICachedEntity = TryCast(obj, _ICachedEntity)

                    Dim fv As IDBValueFilter = TryCast(oschema, IDBValueFilter)
                    Dim value As Object = dr.GetValue(i)
                    If fv IsNot Nothing Then
                        value = fv.CreateValue(se.PropertyAlias, obj, value)
                    End If

                    obj.BeginLoading()
                    ParseValueFromDb(dr, att, i, obj, pi, se.PropertyAlias, oschema, value, ce, _
                                     False, fac, c)

                    obj.EndLoading()

                    Dim cnt As Integer
                    pkdic.TryGetValue(se.ObjectSource, cnt)
                    pkdic(se.ObjectSource) = cnt + 1

                    Dim f As IFactory = TryCast(obj, IFactory)
                    If f IsNot Nothing Then
                        For Each p As Pair(Of String, Object) In fac
                            Dim e As _IEntity = f.CreateObject(p.First, p.Second)
                            If e IsNot Nothing Then
                                e.SetMgrString(IdentityString)
                                If obj.CreateManager IsNot Nothing Then
                                    e.SetCreateManager(obj.CreateManager)
                                End If
                            End If
                        Next
                    End If

                End If
            Next

            For Each de As DictionaryEntry In odic
                Dim obj As _IEntity = CType(de.Value, _IEntity)
                Dim os As ObjectSource = CType(de.Key, ObjectSource)
                Dim ce As _ICachedEntity = TryCast(obj, _ICachedEntity)

                If ce IsNot Nothing Then
                    ce.CheckIsAllLoaded(MappingEngine, pkdic(os))
                End If

                RaiseObjectLoaded(obj)

                l.Add(obj)
            Next

            values.Add(New ReadOnlyObjectList(Of _IEntity)(l))
        End Sub

        Public Sub QueryObjects(Of T As {_IEntity, New})( _
            ByVal cmd As System.Data.Common.DbCommand, _
            ByVal withLoad As Boolean, ByVal values As IList, _
            ByVal selectList As Generic.List(Of ColumnAttribute), ByVal oschema As IEntitySchema, _
            ByVal fields_idx As Collections.IndexedCollection(Of String, MapField2Column))

            If values Is Nothing Then
                'values = New Generic.List(Of T)
                Throw New ArgumentNullException("values")
            End If

            If cmd Is Nothing Then
                Throw New ArgumentNullException("cmd")
            End If

            Invariant()

            'Dim idx As Integer = -1
            Dim original_type As Type = GetType(T)
            Dim c As OrmCache = TryCast(_cache, OrmCache)
            Dim b As ConnAction = TestConn(cmd)
            Try
                _loadedInLastFetch = 0
                If withLoad AndAlso c IsNot Nothing Then
                    c.BeginTrackDelete(original_type)
                End If

                Dim et As New PerfCounter
                Using dr As System.Data.Common.DbDataReader = cmd.ExecuteReader
                    _exec = et.GetTime

                    'If idx = -1 Then
                    '    Dim pk_name As String = MappingEngine.GetPrimaryKeysName(original_type, False)(0)
                    '    Try
                    '        idx = dr.GetOrdinal(pk_name)
                    '    Catch ex As IndexOutOfRangeException
                    '        If _mcSwitch.TraceError Then
                    '            Trace.WriteLine("Invalid column name " & pk_name & " in " & cmd.CommandText)
                    '            Trace.WriteLine(Environment.StackTrace)
                    '        End If
                    '        Throw New OrmManagerException("Cannot get primary key ordinal", ex)
                    '    End Try
                    'End If

                    'If arr Is Nothing Then arr = Schema.GetSortedFieldList(original_type)

                    'Dim idx As Integer = GetPrimaryKeyIdx(cmd.CommandText, original_type, dr)
                    Dim dic As Generic.IDictionary(Of Object, T) = GetDictionary(Of T)(oschema)
                    Dim il As IListEdit = TryCast(values, IListEdit)
                    If il IsNot Nothing Then
                        values = il.List
                    End If
                    Dim props As IDictionary = Nothing
                    If oschema IsNot Nothing Then
                        props = MappingEngine.GetProperties(original_type, oschema)
                    End If

                    If selectList Is Nothing Then
                        selectList = New List(Of ColumnAttribute)
                        For Each m As MapField2Column In fields_idx
                            Dim clm As New ColumnAttribute(m._propertyAlias, m._newattributes)
                            clm.Column = If(Not String.IsNullOrEmpty(m._columnName), m._columnName, m._propertyAlias)
                            selectList.Add(clm)
                        Next
                        selectList.Sort(Function(c1 As ColumnAttribute, c2 As ColumnAttribute) _
                            dr.GetOrdinal(c1.Column).CompareTo(dr.GetOrdinal(c2.Column)))
                        'For i As Integer = 0 To dr.FieldCount - 1
                        '    Dim clm As New ColumnAttribute(dr.GetName(i))
                        '    selectList.Add(clm)
                        'Next
                    End If

                    Dim ft As New PerfCounter
                    Do While dr.Read
                        LoadFromResultSet(Of T)(withLoad, values, selectList, dr, dic, _loadedInLastFetch, oschema, fields_idx, props)
                    Loop
                    _fetch = ft.GetTime
                End Using
            Finally
                If withLoad AndAlso c IsNot Nothing Then
                    c.EndTrackDelete(original_type)
                End If
                CloseConn(b)
            End Try
        End Sub

        Private Sub AfterLoadingProcess(ByVal dic As IDictionary, ByVal obj As _IEntity, ByRef lock As IDisposable, ByRef ro As _IEntity)
            Dim notFromCache As Boolean = Object.ReferenceEquals(ro, obj)
            ro.CorrectStateAfterLoading(notFromCache)
            'If notFromCache Then
            '    If ro.ObjectState = ObjectState.None OrElse ro.ObjectState = ObjectState.NotLoaded Then
            '        Dim co As _ICachedEntity = TryCast(ro, _ICachedEntity)
            '        If co IsNot Nothing Then
            '            _cache.UnregisterModification(co)
            '            If co.IsPKLoaded Then
            '                ro = NormalizeObject(co, dic)
            '            End If
            '            If lock Then
            '                Threading.Monitor.Exit(dic)
            '                lock = False
            '            End If
            '        End If
            '    End If
            'End If
        End Sub

        Protected Friend Sub LoadFromResultSet(Of T As {_IEntity, New})( _
            ByVal withLoad As Boolean, _
            ByVal values As IList, ByVal selectList As Generic.List(Of ColumnAttribute), _
            ByVal dr As System.Data.Common.DbDataReader, _
            ByVal dic As IDictionary(Of Object, T), ByRef loaded As Integer, _
            ByVal oschema As IEntitySchema, ByVal fields_idx As Collections.IndexedCollection(Of String, MapField2Column), _
            ByVal props As IDictionary)

            'Dim id As Integer = CInt(dr.GetValue(idx))
            'Dim obj As OrmBase = CreateDBObject(Of T)(id, dic, withLoad OrElse AlwaysAdd2Cache OrElse Not ListConverter.IsWeak)
            If GetType(IKeyEntity).IsAssignableFrom(GetType(T)) Then
            Else
            End If
            'Dim obj As _ICachedEntity = CType(CreateEntity(Of T)(), _ICachedEntity)
            'If obj IsNot Nothing Then
            'If _raiseCreated Then
            'RaiseObjectCreated(obj)
            'End If
            Dim lock As IDisposable = Nothing
            Try
                Dim obj As New T
                Dim ro As _IEntity = LoadFromDataReader(obj, dr, selectList, False, 0, CType(dic, System.Collections.IDictionary), True, lock, oschema, fields_idx, props)
                AfterLoadingProcess(CType(dic, System.Collections.IDictionary), obj, lock, ro)
#If DEBUG Then
                Dim ce As CachedEntity = TryCast(ro, CachedEntity)
                If ce IsNot Nothing Then ce.Invariant()
#End If
                Dim orm As _ICachedEntity = TryCast(ro, _ICachedEntity)
                If orm Is Nothing OrElse orm.IsPKLoaded Then
                    values.Add(ro)
                    If ro.IsLoaded Then
                        loaded += 1
                    End If
                End If
            Finally
                If lock IsNot Nothing Then
                    'Threading.Monitor.Exit(dic)
                    lock.Dispose()
                End If
            End Try
            '            If withLoad AndAlso Not _cache.IsDeleted(TryCast(obj, ICachedEntity)) Then
            '                Using obj.GetSyncRoot()
            '                    If obj.ObjectState <> ObjectState.Modified AndAlso obj.ObjectState <> ObjectState.Deleted Then
            '                        'If obj.IsLoaded Then obj.IsLoaded = False

            '                        'If Not obj.IsLoaded Then
            '                        '    obj.ObjectState = ObjectState.NotFoundInDB
            '                        '    RemoveObjectFromCache(obj)
            '                        'Else
            '                        obj.CorrectStateAfterLoading()
            '                        values.Add(obj)
            '                        loaded += 1
            '                        'End If
            '                    ElseIf obj.ObjectState = ObjectState.Modified Then
            '                        GoTo l1
            '                    End If
            '                End Using
            '            Else
            'l1:
            '                values.Add(obj)
            '                If obj.IsLoaded Then
            '                    loaded += 1
            '                End If
            '            End If
            'Else
            'If _mcSwitch.TraceVerbose Then
            '    WriteLine("Attempt to load unallowed object " & GetType(T).Name & " (" & id & ")")
            'End If
            'End If
        End Sub

        Protected Function LoadFromDataReader(ByVal obj As _IEntity, ByVal dr As System.Data.Common.DbDataReader, _
            ByVal selectList As Generic.IList(Of ColumnAttribute), ByVal check_pk As Boolean, ByVal displacement As Integer, _
            ByVal dic As IDictionary, ByVal fromRS As Boolean, ByRef lock As IDisposable, ByVal oschema As IEntitySchema, _
            ByVal propertyMap As Collections.IndexedCollection(Of String, MapField2Column), ByVal props As IDictionary) As _IEntity

            If selectList.Count + displacement > dr.FieldCount Then
                Throw New OrmManagerException(String.Format("Actual field count({0}) in query does not satisfy requested fields({1})", dr.FieldCount, selectList.Count))
            End If

            Debug.Assert(obj.ObjectState <> ObjectState.Deleted)

            obj.SetMgrString(IdentityString)

            Dim original_type As Type = obj.GetType
            Dim fv As IDBValueFilter = TryCast(oschema, IDBValueFilter)
            'Dim fields_idx As Collections.IndexedCollection(Of String, MapField2Column) = oschema.GetFieldColumnMap
            Dim fac As New List(Of Pair(Of String, Object))
            Dim ce As _ICachedEntity = TryCast(obj, _ICachedEntity)
            'Dim load As Boolean = a
            'Using obj.GetSyncRoot()
            'Dim d As IDisposable = Nothing
            obj.BeginLoading()
            Dim loading As Boolean = False
            Try
                Dim pk_count As Integer = 0
                Dim pi_cache(selectList.Count - 1) As Reflection.PropertyInfo
                Dim attrs(selectList.Count - 1) As Field2DbRelations
                Dim oldpk() As PKDesc = Nothing
                If ce IsNot Nothing AndAlso Not fromRS Then oldpk = ce.GetPKValues()
                For idx As Integer = 0 To selectList.Count - 1
                    Dim c As ColumnAttribute = selectList(idx)
                    Dim pi As Reflection.PropertyInfo = If(props IsNot Nothing, CType(props(c), Reflection.PropertyInfo), Nothing)
                    pi_cache(idx) = pi
                    Dim propertyAlias As String = c.PropertyAlias
                    If Not String.IsNullOrEmpty(propertyAlias) AndAlso propertyMap.ContainsKey(propertyAlias) Then
                        Dim attr As Field2DbRelations = propertyMap(propertyAlias).GetAttributes(c)
                        attrs(idx) = attr
                        If idx >= 0 AndAlso (attr And Field2DbRelations.PK) = Field2DbRelations.PK Then
                            Assert(idx + displacement < dr.FieldCount, propertyAlias)
                            'If dr.FieldCount <= idx + displacement Then
                            '    If _mcSwitch.TraceError Then
                            '        Dim dt As System.Data.DataTable = dr.GetSchemaTable
                            '        Dim sb As New StringBuilder
                            '        For Each drow As System.Data.DataRow In dt.Rows
                            '            If sb.Length > 0 Then
                            '                sb.Append(", ")
                            '            End If
                            '            sb.Append(drow("ColumnName")).Append("(").Append(drow("ColumnOrdinal")).Append(")")
                            '        Next
                            '        WriteLine(sb.ToString)
                            '    End If
                            'End If
                            pk_count += 1
                            Dim value As Object = dr.GetValue(idx + displacement)
                            If fv IsNot Nothing Then
                                value = fv.CreateValue(propertyAlias, obj, value)
                            End If

                            If Not dr.IsDBNull(idx + displacement) Then
                                'If ce IsNot Nothing AndAlso obj.ObjectState = ObjectState.Created Then
                                '    ce.CreateCopyForSaveNewEntry()
                                '    'bl = True
                                'End If

                                Try
                                    If pi Is Nothing Then
                                        obj.SetValueOptimized(pi, propertyAlias, oschema, value)
                                        If ce IsNot Nothing Then ce.SetLoaded(c, True, True, MappingEngine)
                                    Else
                                        Dim propType As Type = pi.PropertyType
                                        If (propType Is GetType(Boolean) AndAlso value.GetType Is GetType(Short)) OrElse (propType Is GetType(Integer) AndAlso value.GetType Is GetType(Long)) Then
                                            Dim v As Object = Convert.ChangeType(value, propType)
                                            obj.SetValueOptimized(pi, propertyAlias, oschema, v)
                                            If ce IsNot Nothing Then ce.SetLoaded(c, True, True, MappingEngine)
                                        ElseIf propType Is GetType(Byte()) AndAlso value.GetType Is GetType(Date) Then
                                            Dim dt As DateTime = CDate(value)
                                            Dim l As Long = dt.ToBinary
                                            Using ms As New IO.MemoryStream
                                                Dim sw As New IO.StreamWriter(ms)
                                                sw.Write(l)
                                                sw.Flush()
                                                obj.SetValueOptimized(pi, propertyAlias, oschema, ms.ToArray)
                                                If ce IsNot Nothing Then ce.SetLoaded(c, True, True, MappingEngine)
                                            End Using
                                        Else
                                            'If c.FieldName = "ID" Then
                                            '    obj.Identifier = CInt(value)
                                            'Else
                                            obj.SetValueOptimized(pi, propertyAlias, oschema, value)
                                            'End If
                                            If ce IsNot Nothing Then ce.SetLoaded(c, True, True, MappingEngine)
                                        End If
                                    End If
                                Catch ex As ArgumentException When ex.Message.StartsWith("Object of type 'System.DateTime' cannot be converted to type 'System.Byte[]'")
                                    Dim dt As DateTime = CDate(value)
                                    Dim l As Long = dt.ToBinary
                                    Using ms As New IO.MemoryStream
                                        Dim sw As New IO.StreamWriter(ms)
                                        sw.Write(l)
                                        sw.Flush()
                                        obj.SetValueOptimized(pi, propertyAlias, oschema, ms.ToArray)
                                        If ce IsNot Nothing Then ce.SetLoaded(c, True, True, MappingEngine)
                                    End Using
                                Catch ex As ArgumentException When ex.Message.IndexOf("cannot be converted") > 0 AndAlso pi IsNot Nothing
                                    Dim v As Object = Convert.ChangeType(value, pi.PropertyType)
                                    obj.SetValueOptimized(pi, propertyAlias, oschema, v)
                                    If ce IsNot Nothing Then ce.SetLoaded(c, True, True, MappingEngine)
                                End Try
                            End If
                        End If
                    End If
                Next

                If pk_count > 0 Then
                    If ce IsNot Nothing Then
                        ce.PKLoaded(pk_count)

                        Dim c As OrmCache = TryCast(_cache, OrmCache)
                        If c IsNot Nothing AndAlso c.IsDeleted(ce) Then
                            Return obj
                        End If

                        'Threading.Monitor.Enter(dic)
                        'lock = True

                        If dic IsNot Nothing Then
                            Dim robj As ICachedEntity = NormalizeObject(ce, dic, fromRS)
                            Dim fromCache As Boolean = Not Object.ReferenceEquals(robj, ce)

                            obj = robj
                            ce = CType(obj, _ICachedEntity)
                            SyncLock dic
                                If fromCache Then
                                    If obj.ObjectState = ObjectState.Created Then
                                        Using obj.GetSyncRoot
                                            Return obj
                                        End Using
                                    ElseIf obj.ObjectState = ObjectState.Modified OrElse obj.ObjectState = ObjectState.Deleted Then
                                        Return obj
                                    Else
                                        obj.BeginLoading()
                                    End If
                                Else
                                    If fromRS Then
                                    Else
                                        If obj.ObjectState = ObjectState.Created Then
                                            ce.CreateCopyForSaveNewEntry(Me, oldpk)
                                            'Cache.Modified(obj).Reason = ModifiedObject.ReasonEnum.SaveNew
                                        End If
                                    End If
                                End If
                            End SyncLock
                        End If
                    End If
                ElseIf ce IsNot Nothing AndAlso Not fromRS AndAlso obj.ObjectState = ObjectState.Created Then
                    ce.CreateCopyForSaveNewEntry(Me, Nothing)
                End If

                If pk_count < selectList.Count Then
                    lock = obj.GetSyncRoot
                    If obj.ObjectState = ObjectState.Deleted OrElse obj.ObjectState = ObjectState.NotFoundInSource Then
                        Return obj
                    End If

                    For idx As Integer = 0 To selectList.Count - 1
                        Dim c As ColumnAttribute = selectList(idx)
                        Dim pi As Reflection.PropertyInfo = pi_cache(idx) '_schema.GetProperty(original_type, c)
                        Dim propertyAlias As String = c.PropertyAlias

                        Dim value As Object = dr.GetValue(idx + displacement)
                        If fv IsNot Nothing Then
                            value = fv.CreateValue(propertyAlias, obj, value)
                        End If

                        If String.IsNullOrEmpty(propertyAlias) Then
                            propertyAlias = c.Column
                        End If

                        If String.IsNullOrEmpty(propertyAlias) Then
                            Continue For
                        End If

                        Dim att As Field2DbRelations = attrs(idx + displacement)

                        ParseValueFromDb(dr, att, idx + displacement, obj, pi, propertyAlias, oschema, value, _
                                         ce, check_pk, fac, c)
                        'If pi Is Nothing Then
                        '    If dr.IsDBNull(idx + displacement) Then
                        '        obj.SetValueOptimized(pi, propertyAlias, oschema, Nothing)
                        '    Else
                        '        obj.SetValueOptimized(pi, propertyAlias, oschema, value)
                        '    End If
                        '    If ce IsNot Nothing Then ce.SetLoaded(c, True, False, MappingEngine)
                        'Else
                        '    Dim att As Field2DbRelations = attrs(idx)
                        '    Dim propType As Type = pi.PropertyType
                        '    If check_pk AndAlso (att And Field2DbRelations.PK) = Field2DbRelations.PK Then
                        '        Dim v As Object = pi.GetValue(obj, Nothing)
                        '        If Not value.GetType Is propType AndAlso propType IsNot GetType(Object) Then
                        '            value = Convert.ChangeType(value, propType)
                        '        End If
                        '        If Not v.Equals(value) Then
                        '            Throw New OrmManagerException("PK values is not equals (" & dr.GetName(idx + displacement) & "): value from db: " & value.ToString & "; value from object: " & v.ToString)
                        '        End If
                        '    ElseIf Not dr.IsDBNull(idx + displacement) AndAlso (att And Field2DbRelations.PK) <> Field2DbRelations.PK Then
                        '        If (att And Field2DbRelations.Factory) = Field2DbRelations.Factory Then
                        '            fac.Add(New Pair(Of String, Object)(propertyAlias, value))
                        '            If ce IsNot Nothing Then ce.SetLoaded(c, True, True, MappingEngine)
                        '            '    'obj.CreateObject(c.FieldName, value)
                        '            '    obj.SetValue(pi, c, )
                        '            '    obj.SetLoaded(c, True, True)
                        '            '    'If GetType(OrmBase) Is pi.PropertyType Then
                        '            '    '    obj.CreateObject(CInt(value))
                        '            '    '    obj.SetLoaded(c, True)
                        '            '    'Else
                        '            '    '    Dim type_created As Type = pi.PropertyType
                        '            '    '    Dim o As OrmBase = CreateDBObject(CInt(value), type_created)
                        '            '    '    obj.SetValue(pi, c, o)
                        '            '    '    obj.SetLoaded(c, True)
                        '            '    'End If
                        '        ElseIf GetType(IKeyEntity).IsAssignableFrom(propType) Then
                        '            Dim type_created As Type = propType
                        '            Dim en As String = MappingEngine.GetEntityNameByType(type_created)
                        '            If Not String.IsNullOrEmpty(en) Then
                        '                type_created = MappingEngine.GetTypeByEntityName(en)

                        '                If type_created Is Nothing Then
                        '                    Throw New OrmManagerException("Cannot find type for entity " & en)
                        '                End If
                        '            End If
                        '            Dim o As IKeyEntity = GetOrmBaseFromCacheOrCreate(value, type_created)
                        '            obj.SetValueOptimized(pi, propertyAlias, oschema, o)
                        '            If o IsNot Nothing Then
                        '                o.SetCreateManager(obj.CreateManager)
                        '            End If
                        '            If ce IsNot Nothing Then ce.SetLoaded(c, True, True, MappingEngine)
                        '        ElseIf GetType(System.Xml.XmlDocument) Is propType AndAlso TypeOf (value) Is String Then
                        '            Dim o As New System.Xml.XmlDocument
                        '            o.LoadXml(CStr(value))
                        '            obj.SetValueOptimized(pi, propertyAlias, oschema, o)
                        '            If ce IsNot Nothing Then ce.SetLoaded(c, True, True, MappingEngine)
                        '        ElseIf propType.IsEnum AndAlso TypeOf (value) Is String Then
                        '            Dim svalue As String = CStr(value).Trim
                        '            If svalue = String.Empty Then
                        '                obj.SetValueOptimized(pi, propertyAlias, oschema, 0)
                        '                If ce IsNot Nothing Then ce.SetLoaded(c, True, True, MappingEngine)
                        '            Else
                        '                obj.SetValueOptimized(pi, propertyAlias, oschema, [Enum].Parse(propType, svalue, True))
                        '                If ce IsNot Nothing Then ce.SetLoaded(c, True, True, MappingEngine)
                        '            End If
                        '        ElseIf propType.IsGenericType AndAlso GetType(Nullable(Of )).Name = propType.Name Then
                        '            Dim t As Type = propType.GetGenericArguments()(0)
                        '            Dim v As Object = Nothing
                        '            If t.IsPrimitive Then
                        '                v = Convert.ChangeType(value, t)
                        '            ElseIf t.IsEnum Then
                        '                If TypeOf (value) Is String Then
                        '                    Dim svalue As String = CStr(value).Trim
                        '                    If svalue = String.Empty Then
                        '                        v = [Enum].ToObject(t, 0)
                        '                    Else
                        '                        v = [Enum].Parse(t, svalue, True)
                        '                    End If
                        '                Else
                        '                    v = [Enum].ToObject(t, value)
                        '                End If
                        '            ElseIf t Is value.GetType Then
                        '                v = value
                        '            Else
                        '                Try
                        '                    v = t.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, _
                        '                        Nothing, Nothing, New Object() {value})
                        '                Catch ex As MissingMethodException
                        '                    'Debug.WriteLine(c.FieldName & ": " & original_type.Name)
                        '                    'v = Convert.ChangeType(value, t)
                        '                End Try
                        '            End If
                        '            Dim v2 As Object = propType.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, _
                        '                Nothing, Nothing, New Object() {v})
                        '            obj.SetValueOptimized(pi, propertyAlias, oschema, v2)
                        '            If ce IsNot Nothing Then ce.SetLoaded(c, True, True, MappingEngine)
                        '        Else
                        '            Try
                        '                If (propType.IsPrimitive AndAlso value.GetType.IsPrimitive) OrElse (propType Is GetType(Long) AndAlso value.GetType Is GetType(Decimal)) Then
                        '                    Dim v As Object = Convert.ChangeType(value, propType)
                        '                    obj.SetValueOptimized(pi, propertyAlias, oschema, v)
                        '                    If ce IsNot Nothing Then ce.SetLoaded(c, True, True, MappingEngine)
                        '                ElseIf propType Is GetType(Byte()) AndAlso value.GetType Is GetType(Date) Then
                        '                    Dim dt As DateTime = CDate(value)
                        '                    Dim l As Long = dt.ToBinary
                        '                    Using ms As New IO.MemoryStream
                        '                        Dim sw As New IO.StreamWriter(ms)
                        '                        sw.Write(l)
                        '                        sw.Flush()
                        '                        'pi.SetValue(obj, ms.ToArray, Nothing)
                        '                        obj.SetValueOptimized(pi, propertyAlias, oschema, ms.ToArray)
                        '                        If ce IsNot Nothing Then ce.SetLoaded(c, True, True, MappingEngine)
                        '                    End Using
                        '                    'ElseIf pi.PropertyType Is GetType(ReleaseDate) AndAlso value.GetType Is GetType(Integer) Then
                        '                    '    obj.SetValue(pi, c, pi.PropertyType.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, _
                        '                    '        Nothing, New Object() {value}))
                        '                    '    obj.SetLoaded(c, True)
                        '                Else
                        '                    obj.SetValueOptimized(pi, propertyAlias, oschema, value)
                        '                    If ce IsNot Nothing Then ce.SetLoaded(c, True, True, MappingEngine)
                        '                End If
                        '                'Catch ex As ArgumentException When ex.Message.StartsWith("Object of type 'System.DateTime' cannot be converted to type 'System.Byte[]'")
                        '                '    Dim dt As DateTime = CDate(value)
                        '                '    Dim l As Long = dt.ToBinary
                        '                '    Using ms As New IO.MemoryStream
                        '                '        Dim sw As New IO.StreamWriter(ms)
                        '                '        sw.Write(l)
                        '                '        sw.Flush()
                        '                '        obj.SetValue(pi, c, ms.ToArray)
                        '                '        obj.SetLoaded(c, True)
                        '                '    End Using
                        '            Catch ex As ArgumentException When ex.Message.IndexOf("cannot be converted") > 0
                        '                Dim v As Object = Convert.ChangeType(value, propType)
                        '                obj.SetValueOptimized(pi, propertyAlias, oschema, v)
                        '                If ce IsNot Nothing Then ce.SetLoaded(c, True, True, MappingEngine)
                        '            End Try
                        '        End If
                        '    ElseIf dr.IsDBNull(idx + displacement) Then
                        '        obj.SetValueOptimized(pi, propertyAlias, oschema, Nothing)
                        '        If ce IsNot Nothing Then ce.SetLoaded(c, True, True, MappingEngine)
                        '    End If
                        'End If
                    Next

                    Dim f As IFactory = TryCast(obj, IFactory)
                    If f IsNot Nothing Then
                        For Each p As Pair(Of String, Object) In fac
                            Dim e As _IEntity = f.CreateObject(p.First, p.Second)
                            If e IsNot Nothing Then
                                e.SetMgrString(IdentityString)
                                RaiseObjectLoaded(e)
                                'If obj.CreateManager IsNot Nothing Then
                                '    e.SetCreateManager(obj.CreateManager)
                                'End If
                            End If
                        Next
                    End If
                    'Finally
                    '    obj.EndLoading()
                    '    loading = True
                    'End Try
                    'End Using
                End If
            Finally
                If Not loading Then obj.EndLoading()
            End Try

            If ce IsNot Nothing Then
                ce.CheckIsAllLoaded(MappingEngine, selectList.Count)
            End If

            RaiseObjectLoaded(obj)
            Return obj
        End Function

        Public Function ParseValueFromDb(ByVal dr As System.Data.Common.DbDataReader, _
            ByVal att As Field2DbRelations, ByVal i As Integer, ByVal obj As _IEntity, _
            ByVal pi As Reflection.PropertyInfo, ByVal propertyAlias As String, _
            ByVal oschema As IEntitySchema, ByVal value As Object, ByVal ce As _ICachedEntity, _
            ByVal check_pk As Boolean, ByVal fac As List(Of Pair(Of String, Object)), _
            ByVal c As ColumnAttribute) As Object
            If pi Is Nothing Then
                If dr.IsDBNull(i) Then
                    obj.SetValueOptimized(pi, propertyAlias, oschema, Nothing)
                Else
                    obj.SetValueOptimized(pi, propertyAlias, oschema, value)
                End If
                If ce IsNot Nothing Then ce.SetLoaded(c, True, False, MappingEngine)
            Else
                Dim propType As Type = pi.PropertyType
                If check_pk AndAlso (att And Field2DbRelations.PK) = Field2DbRelations.PK Then
                    Dim v As Object = pi.GetValue(obj, Nothing)
                    If Not value.GetType Is propType AndAlso propType IsNot GetType(Object) Then
                        value = Convert.ChangeType(value, propType)
                    End If
                    If Not v.Equals(value) Then
                        Throw New OrmManagerException("PK values is not equals (" & dr.GetName(i) & "): value from db: " & value.ToString & "; value from object: " & v.ToString)
                    End If
                ElseIf Not dr.IsDBNull(i) AndAlso (att And Field2DbRelations.PK) <> Field2DbRelations.PK Then
                    If (att And Field2DbRelations.Factory) = Field2DbRelations.Factory Then
                        fac.Add(New Pair(Of String, Object)(propertyAlias, value))
                        If ce IsNot Nothing Then ce.SetLoaded(c, True, True, MappingEngine)
                        '    'obj.CreateObject(c.FieldName, value)
                        '    obj.SetValue(pi, c, )
                        '    obj.SetLoaded(c, True, True)
                        '    'If GetType(OrmBase) Is pi.PropertyType Then
                        '    '    obj.CreateObject(CInt(value))
                        '    '    obj.SetLoaded(c, True)
                        '    'Else
                        '    '    Dim type_created As Type = pi.PropertyType
                        '    '    Dim o As OrmBase = CreateDBObject(CInt(value), type_created)
                        '    '    obj.SetValue(pi, c, o)
                        '    '    obj.SetLoaded(c, True)
                        '    'End If
                    ElseIf GetType(IKeyEntity).IsAssignableFrom(propType) Then
                        Dim type_created As Type = propType
                        Dim en As String = MappingEngine.GetEntityNameByType(type_created)
                        If Not String.IsNullOrEmpty(en) Then
                            type_created = MappingEngine.GetTypeByEntityName(en)

                            If type_created Is Nothing Then
                                Throw New OrmManagerException("Cannot find type for entity " & en)
                            End If
                        End If
                        Dim o As IKeyEntity = GetOrmBaseFromCacheOrCreate(value, type_created)
                        obj.SetValueOptimized(pi, propertyAlias, oschema, o)
                        If o IsNot Nothing Then
                            o.SetCreateManager(obj.CreateManager)
                        End If
                        If ce IsNot Nothing Then ce.SetLoaded(c, True, True, MappingEngine)
                    ElseIf GetType(System.Xml.XmlDocument) Is propType AndAlso TypeOf (value) Is String Then
                        Dim o As New System.Xml.XmlDocument
                        o.LoadXml(CStr(value))
                        obj.SetValueOptimized(pi, propertyAlias, oschema, o)
                        If ce IsNot Nothing Then ce.SetLoaded(c, True, True, MappingEngine)
                    ElseIf propType.IsEnum AndAlso TypeOf (value) Is String Then
                        Dim svalue As String = CStr(value).Trim
                        If svalue = String.Empty Then
                            obj.SetValueOptimized(pi, propertyAlias, oschema, 0)
                            If ce IsNot Nothing Then ce.SetLoaded(c, True, True, MappingEngine)
                        Else
                            obj.SetValueOptimized(pi, propertyAlias, oschema, [Enum].Parse(propType, svalue, True))
                            If ce IsNot Nothing Then ce.SetLoaded(c, True, True, MappingEngine)
                        End If
                    ElseIf propType.IsGenericType AndAlso GetType(Nullable(Of )).Name = propType.Name Then
                        Dim t As Type = propType.GetGenericArguments()(0)
                        Dim v As Object = Nothing
                        If t.IsPrimitive Then
                            v = Convert.ChangeType(value, t)
                        ElseIf t.IsEnum Then
                            If TypeOf (value) Is String Then
                                Dim svalue As String = CStr(value).Trim
                                If svalue = String.Empty Then
                                    v = [Enum].ToObject(t, 0)
                                Else
                                    v = [Enum].Parse(t, svalue, True)
                                End If
                            Else
                                v = [Enum].ToObject(t, value)
                            End If
                        ElseIf t Is value.GetType Then
                            v = value
                        Else
                            Try
                                v = t.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, _
                                    Nothing, Nothing, New Object() {value})
                            Catch ex As MissingMethodException
                                'Debug.WriteLine(c.FieldName & ": " & original_type.Name)
                                'v = Convert.ChangeType(value, t)
                            End Try
                        End If
                        Dim v2 As Object = propType.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, _
                            Nothing, Nothing, New Object() {v})
                        obj.SetValueOptimized(pi, propertyAlias, oschema, v2)
                        If ce IsNot Nothing Then ce.SetLoaded(c, True, True, MappingEngine)
                    Else
                        Try
                            If (propType.IsPrimitive AndAlso value.GetType.IsPrimitive) OrElse (propType Is GetType(Long) AndAlso value.GetType Is GetType(Decimal)) Then
                                Dim v As Object = Convert.ChangeType(value, propType)
                                obj.SetValueOptimized(pi, propertyAlias, oschema, v)
                                If ce IsNot Nothing Then ce.SetLoaded(c, True, True, MappingEngine)
                            ElseIf propType Is GetType(Byte()) AndAlso value.GetType Is GetType(Date) Then
                                Dim dt As DateTime = CDate(value)
                                Dim l As Long = dt.ToBinary
                                Using ms As New IO.MemoryStream
                                    Dim sw As New IO.StreamWriter(ms)
                                    sw.Write(l)
                                    sw.Flush()
                                    'pi.SetValue(obj, ms.ToArray, Nothing)
                                    obj.SetValueOptimized(pi, propertyAlias, oschema, ms.ToArray)
                                    If ce IsNot Nothing Then ce.SetLoaded(c, True, True, MappingEngine)
                                End Using
                                'ElseIf pi.PropertyType Is GetType(ReleaseDate) AndAlso value.GetType Is GetType(Integer) Then
                                '    obj.SetValue(pi, c, pi.PropertyType.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, _
                                '        Nothing, New Object() {value}))
                                '    obj.SetLoaded(c, True)
                            Else
                                obj.SetValueOptimized(pi, propertyAlias, oschema, value)
                                If ce IsNot Nothing Then ce.SetLoaded(c, True, True, MappingEngine)
                            End If
                            'Catch ex As ArgumentException When ex.Message.StartsWith("Object of type 'System.DateTime' cannot be converted to type 'System.Byte[]'")
                            '    Dim dt As DateTime = CDate(value)
                            '    Dim l As Long = dt.ToBinary
                            '    Using ms As New IO.MemoryStream
                            '        Dim sw As New IO.StreamWriter(ms)
                            '        sw.Write(l)
                            '        sw.Flush()
                            '        obj.SetValue(pi, c, ms.ToArray)
                            '        obj.SetLoaded(c, True)
                            '    End Using
                        Catch ex As ArgumentException When ex.Message.IndexOf("cannot be converted") > 0
                            Dim v As Object = Convert.ChangeType(value, propType)
                            obj.SetValueOptimized(pi, propertyAlias, oschema, v)
                            If ce IsNot Nothing Then ce.SetLoaded(c, True, True, MappingEngine)
                        End Try
                    End If
                ElseIf dr.IsDBNull(i) Then
                    obj.SetValueOptimized(pi, propertyAlias, oschema, Nothing)
                    If ce IsNot Nothing Then ce.SetLoaded(c, True, True, MappingEngine)
                End If
            End If
        End Function

        Protected Friend Function TestConn(ByVal cmd As System.Data.Common.DbCommand) As ConnAction
            Invariant()

            Dim r As ConnAction

            If _conn Is Nothing Then
                _conn = CreateConn()
                _conn.Open()
                r = ConnAction.Destroy
                If cmd IsNot Nothing Then cmd.Connection = _conn
            Else
                If _tran IsNot Nothing Then
                    If cmd IsNot Nothing Then
                        cmd.Connection = _conn
                        cmd.Transaction = _tran
                    End If
                Else
                    Dim cs As System.Data.ConnectionState = _conn.State
                    If cs = System.Data.ConnectionState.Closed OrElse cs = System.Data.ConnectionState.Broken Then
                        _conn.Open()
                        r = ConnAction.Close
                    End If
                    If cmd IsNot Nothing Then cmd.Connection = _conn
                End If
            End If

            TraceStmt(cmd)
            Return r
        End Function

        Protected Friend Sub CloseConn(ByVal b As ConnAction)
            Invariant()

            Assert(_conn IsNot Nothing, "Connection cannot be nothing")

            Select Case b
                Case ConnAction.Close
                    _conn.Close()
                Case ConnAction.Destroy
                    _conn.Dispose()
                    _conn = Nothing
            End Select
        End Sub

        '<Conditional("TRACE")> _
        Protected Sub TraceStmt(ByVal cmd As System.Data.Common.DbCommand)
            Dim sb As New StringBuilder
            If _tsStmt.Switch.ShouldTrace(TraceEventType.Information) Then
                'SyncLock _tsStmt
                For Each p As System.Data.Common.DbParameter In cmd.Parameters
                    With p
                        Dim t As Type = GetType(DBNull)
                        Dim val As String = "null"
                        Dim tp As String = "nvarchar(1)"
                        If p.Value IsNot Nothing AndAlso p.Value IsNot DBNull.Value Then
                            t = p.Value.GetType
                            val = p.Value.ToString
                            If TypeOf p.Value Is String Then
                                val = "'" & val & "'"
                            End If
                            tp = DbTypeConvertor.ToSqlDbType(p.DbType).ToString
                            If p.DbType = System.Data.DbType.String Then
                                If p.Size = 0 Then
                                    tp &= "(1)"
                                Else
                                    tp &= "(" & p.Size.ToString & ")"
                                End If
                            End If
                        End If
                        sb.Append(SQLGenerator.DeclareVariable(p.ParameterName, tp))
                        sb.AppendLine(";set " & p.ParameterName & " = " & val)
                    End With
                Next
                sb.AppendLine(cmd.CommandText)
                'End SyncLock
            End If
            helper.WriteInfo(_tsStmt, sb.ToString)
            WriteLineInfo(sb.ToString)
        End Sub

        'Protected Friend Overrides Function LoadObjectsInternal(Of T As {OrmBase, New})( _
        '    ByVal objs As ReadOnlyList(Of T), ByVal start As Integer, ByVal length As Integer, _
        '    ByVal remove_not_found As Boolean) As ReadOnlyList(Of T)

        '    'Invariant()

        '    'If objs.Count < 1 Then
        '    '    Return objs
        '    'End If

        '    'If start > objs.Count Then
        '    '    Throw New ArgumentException(String.Format("The range {0},{1} is greater than array length: " & objs.Count, start, length))
        '    'End If

        '    'length = Math.Min(length, objs.Count - start)

        '    'Dim ids As Generic.List(Of Integer) = FormPKValues(Of T)(Me, objs, start, length, True, columns)
        '    'If ids.Count < 1 Then
        '    '    Return objs
        '    'End If

        '    'Dim original_type As Type = GetType(T)
        '    'Dim almgr As AliasMgr = AliasMgr.Create
        '    'Dim params As New ParamMgr(DbSchema, "p")
        '    'Dim sb As New StringBuilder
        '    'sb.Append(DbSchema.Select(original_type, almgr, params, columns, Nothing, GetFilterInfo))
        '    'If Not DbSchema.AppendWhere(original_type, Nothing, almgr, sb, GetFilterInfo, params) Then
        '    '    sb.Append(" where 1=1 ")
        '    'End If
        '    'Dim values As New Generic.List(Of T)
        '    'Dim pcnt As Integer = params.Params.Count
        '    'Dim nextp As Integer = pcnt
        '    'For Each cmd_str As Pair(Of String, Integer) In GetFilters(ids, "ID", almgr, params, original_type, True)
        '    '    Dim sb_cmd As New StringBuilder
        '    '    sb_cmd.Append(sb.ToString).Append(cmd_str.First)

        '    '    Using cmd As System.Data.Common.DbCommand = DbSchema.CreateDBCommand
        '    '        With cmd
        '    '            .CommandType = System.Data.CommandType.Text
        '    '            .CommandText = sb_cmd.ToString
        '    '            params.AppendParams(.Parameters, 0, pcnt)
        '    '            params.AppendParams(.Parameters, nextp, cmd_str.Second - nextp)
        '    '            nextp = cmd_str.Second
        '    '        End With
        '    '        LoadMultipleObjects(Of T)(cmd, True, values, columns)
        '    '    End Using
        '    'Next

        '    'values.Clear()
        '    'For Each o As T In objs
        '    '    If o.IsLoaded Then
        '    '        values.Add(o)
        '    '    End If
        '    'Next
        '    'Return New ReadOnlyList(Of T)(values)

        '    Dim original_type As Type = GetType(T)
        '    Dim columns As Generic.List(Of ColumnAttribute) = _schema.GetSortedFieldList(original_type)

        '    Return LoadObjectsInternal(Of T)(objs, start, length, remove_not_found, columns, True)
        'End Function

        Public Overrides Function LoadObjectsInternal(Of T As {IKeyEntity, New}, T2 As {IKeyEntity})( _
            ByVal objs As ReadOnlyList(Of T2), ByVal start As Integer, ByVal length As Integer, _
            ByVal remove_not_found As Boolean, ByVal columns As Generic.List(Of ColumnAttribute), _
            ByVal withLoad As Boolean) As ReadOnlyList(Of T2)
            Invariant()

            If objs.Count < 1 Then
                Return objs
            End If

            If start > objs.Count Then
                Throw New ArgumentException(String.Format("The range {0},{1} is greater than array length: " & objs.Count, start, length))
            End If

            length = Math.Min(length, objs.Count - start)

            Dim ids As Generic.List(Of Object) = FormPKValues(Of T2)(_cache, objs, start, length)
            If ids.Count < 1 Then
                'Dim l As New List(Of T)
                'For Each o As T In objs
                '    l.Add(o)
                'Next
                'Return l
                Return objs
            End If

            Dim original_type As Type = GetType(T)
            Dim almgr As AliasMgr = AliasMgr.Create
            Dim params As New ParamMgr(SQLGenerator, "p")
            Dim sb As New StringBuilder
            sb.Append(SQLGenerator.Select(MappingEngine, original_type, almgr, params, columns, Nothing, GetFilterInfo))
            If Not SQLGenerator.AppendWhere(MappingEngine, original_type, Nothing, almgr, sb, GetFilterInfo, params) Then
                sb.Append(" where 1=1 ")
            End If
            Dim values As New Generic.List(Of T2)
            Dim pcnt As Integer = params.Params.Count
            Dim nextp As Integer = pcnt
            Dim pkname As String = MappingEngine.GetPrimaryKeys(original_type)(0).PropertyAlias
            For Each cmd_str As Pair(Of String, Integer) In GetFilters(ids, pkname, almgr, params, original_type, True)
                Dim sb_cmd As New StringBuilder
                sb_cmd.Append(sb.ToString).Append(cmd_str.First)

                Using cmd As System.Data.Common.DbCommand = CreateDBCommand()
                    With cmd
                        .CommandType = System.Data.CommandType.Text
                        .CommandText = sb_cmd.ToString
                        params.AppendParams(.Parameters, 0, pcnt)
                        params.AppendParams(.Parameters, nextp, cmd_str.Second - nextp)
                        nextp = cmd_str.Second
                    End With
                    LoadMultipleObjects(Of T)(cmd, True, values, columns)
                End Using
            Next

            Dim vdic As New Dictionary(Of T2, T2)
            For Each v As T2 In values
                vdic.Add(v, v)
            Next
            Dim result As New ReadOnlyList(Of T2)(original_type)
            Dim ar As IListEdit = result
            'Dim dic As IDictionary(Of Object, T) = GetDictionary(Of T)()
            If remove_not_found Then
                'For Each o As T In objs
                '    If Not withLoad Then
                '        If values.Contains(o) OrElse Not ids.Contains(o.Identifier) Then
                '            ar.Add(o)
                '        Else
                '            o.SetObjectState(ObjectState.NotFoundInSource)
                '        End If
                '    Else
                '        If o.IsLoaded Then
                '            ar.Add(o)
                '        ElseIf ListConverter.IsWeak Then
                '            Dim obj As T = Nothing
                '            If dic.TryGetValue(o.Key, obj) AndAlso (o.IsLoaded OrElse values.Contains(o)) Then
                '                ar.Add(obj)
                '            Else
                '                Dim idx As Integer = values.IndexOf(o)
                '                If idx >= 0 Then
                '                    Dim ro As T = values(idx)
                '                    Debug.Assert(ro.IsLoaded)
                '                    Add2Cache(ro)
                '                    ar.Add(ro)
                '                End If
                '            End If
                '        Else

                '        End If
                '    End If
                'Next
                Dim i As Integer = 0
                For Each o As T2 In objs
                    If i >= start AndAlso i < start + length Then
                        Dim ro As T2 = Nothing
                        If vdic.TryGetValue(o, ro) Then
                            If withLoad Then
                                If ro.IsLoaded Then
                                    ar.Add(ro)
                                End If
                            Else
                                ar.Add(ro)
                            End If
                        Else
                            If o.ObjectState <> ObjectState.NotFoundInSource AndAlso o.ObjectState <> ObjectState.Created Then
                                ar.Add(o)
                            End If
                        End If
                    Else
                        If o.ObjectState <> ObjectState.NotFoundInSource AndAlso o.ObjectState <> ObjectState.Created Then
                            ar.Add(o)
                        End If
                    End If
                    i += 1
                Next
            Else
                If vdic.Count = 0 Then
                    result = New ReadOnlyList(Of T2)(original_type, objs)
                Else
                    Dim i As Integer = 0
                    For Each o As T2 In objs
                        If i >= start AndAlso i < start + length Then
                            Dim ro As T2 = Nothing
                            If vdic.TryGetValue(o, ro) Then
                                ar.Add(ro)
                            Else
                                ar.Add(o)
                            End If
                        Else
                            ar.Add(o)
                        End If
                    Next
                End If

                'If ListConverter.IsWeak Then
                '    For Each o As T In objs
                '        Dim obj As T = Nothing
                '        If dic.TryGetValue(o.Key, obj) Then
                '            ar.Add(obj)
                '        Else
                '            ar.Add(o)
                '        End If
                '    Next
                'Else
                '    Return objs
                'End If
            End If
            Return result
            'Return New ReadOnlyList(Of T)(values)
        End Function

        Public Overrides Function LoadObjectsInternal(Of T2 As {IKeyEntity})(ByVal realType As Type, _
            ByVal objs As ReadOnlyList(Of T2), ByVal start As Integer, ByVal length As Integer, _
            ByVal remove_not_found As Boolean, ByVal columns As Generic.List(Of ColumnAttribute), _
            ByVal withLoad As Boolean) As ReadOnlyList(Of T2)
            Invariant()

            If objs.Count < 1 Then
                Return objs
            End If

            If start > objs.Count Then
                Throw New ArgumentException(String.Format("The range {0},{1} is greater than array length: " & objs.Count, start, length))
            End If

            length = Math.Min(length, objs.Count - start)

            Dim ids As Generic.List(Of Object) = FormPKValues(Of T2)(_cache, objs, start, length)
            If ids.Count < 1 Then
                'Dim l As New List(Of T)
                'For Each o As T In objs
                '    l.Add(o)
                'Next
                'Return l
                Return objs
            End If

            'Dim original_type As Type = realType
            Dim almgr As AliasMgr = AliasMgr.Create
            Dim params As New ParamMgr(SQLGenerator, "p")
            Dim sb As New StringBuilder
            sb.Append(SQLGenerator.Select(MappingEngine, realType, almgr, params, columns, Nothing, GetFilterInfo))
            If Not SQLGenerator.AppendWhere(MappingEngine, realType, Nothing, almgr, sb, GetFilterInfo, params) Then
                sb.Append(" where 1=1 ")
            End If
            Dim values As New Generic.List(Of T2)
            Dim pcnt As Integer = params.Params.Count
            Dim nextp As Integer = pcnt
            Dim pkname As String = MappingEngine.GetPrimaryKeys(realType)(0).PropertyAlias
            For Each cmd_str As Pair(Of String, Integer) In GetFilters(ids, pkname, almgr, params, realType, True)
                Dim sb_cmd As New StringBuilder
                sb_cmd.Append(sb.ToString).Append(cmd_str.First)

                Using cmd As System.Data.Common.DbCommand = CreateDBCommand()
                    With cmd
                        .CommandType = System.Data.CommandType.Text
                        .CommandText = sb_cmd.ToString
                        params.AppendParams(.Parameters, 0, pcnt)
                        params.AppendParams(.Parameters, nextp, cmd_str.Second - nextp)
                        nextp = cmd_str.Second
                    End With
                    LoadMultipleObjects(realType, cmd, True, values, columns)
                End Using
            Next

            Dim result As New ReadOnlyList(Of T2)(realType)
            Dim ar As IListEdit = result
            'Dim dic As IDictionary(Of Integer, T) = GetDictionary(realtype)
            If remove_not_found Then
                'For Each o As T In objs
                '    If Not withLoad Then
                '        If values.Contains(o) OrElse Not ids.Contains(o.Identifier) Then
                '            ar.Add(o)
                '        Else
                '            o.SetObjectState(ObjectState.NotFoundInSource)
                '        End If
                '    Else
                '        If o.IsLoaded Then
                '            ar.Add(o)
                '        ElseIf ListConverter.IsWeak Then
                '            Dim obj As T = Nothing
                '            If dic.TryGetValue(o.Key, obj) AndAlso (o.IsLoaded OrElse values.Contains(o)) Then
                '                ar.Add(obj)
                '            Else
                '                Dim idx As Integer = values.IndexOf(o)
                '                If idx >= 0 Then
                '                    Dim ro As T = values(idx)
                '                    Debug.Assert(ro.IsLoaded)
                '                    Add2Cache(ro)
                '                    ar.Add(ro)
                '                End If
                '            End If
                '        Else

                '        End If
                '    End If
                'Next
                For Each o As T2 In objs
                    Dim pos As Integer = values.IndexOf(o)
                    If pos >= 0 Then
                        If withLoad Then
                            If values(pos).IsLoaded Then
                                ar.Add(values(pos))
                            End If
                        Else
                            ar.Add(values(pos))
                        End If
                    End If
                Next
            Else
                result = New ReadOnlyList(Of T2)(realType, values)
                'If ListConverter.IsWeak Then
                '    For Each o As T In objs
                '        Dim obj As T = Nothing
                '        If dic.TryGetValue(o.Key, obj) Then
                '            ar.Add(obj)
                '        Else
                '            ar.Add(o)
                '        End If
                '    Next
                'Else
                '    Return objs
                'End If
            End If
            Return result
            'Return New ReadOnlyList(Of T)(values)
        End Function

        Protected Function GetFilters(ByVal ids_ As Generic.List(Of Object), ByVal fieldName As String, _
            ByVal almgr As AliasMgr, ByVal params As ParamMgr, ByVal original_type As Type, ByVal idsSorted As Boolean) As Generic.IEnumerable(Of Pair(Of String, Integer))

            Dim mr As MergeResult = Nothing
            Dim l As New Generic.List(Of Pair(Of String, Integer))

            If ids_.Count > 0 Then
                Dim int As Integer
                Dim ids As Generic.List(Of Integer) = Nothing
                If Integer.TryParse(ids_(0).ToString, int) Then
                    ids = ids_.ConvertAll(Function(i) Convert.ToInt32(i))
                    mr = MergeIds(ids, Not idsSorted)
                Else
                    Dim sb As New StringBuilder
                    For Each o As Object In ids_
                        sb.Append(o.ToString).Append(",")
                    Next
                    sb.Length -= 1
                    Dim f As New cc.EntityFilter(original_type, fieldName, New LiteralValue(sb.ToString), Worm.Criteria.FilterOperation.In)
                    l.Add(New Pair(Of String, Integer)(f.MakeQueryStmt(MappingEngine, SQLGenerator, GetFilterInfo, almgr, params, Nothing), params.Params.Count))
                End If
            End If

            If mr IsNot Nothing Then
                Dim sb As New StringBuilder

                For Each p As Pair(Of Integer) In mr.Pairs
                    Dim con As New Condition.ConditionConstructor
                    con.AddFilter(New cc.EntityFilter(original_type, fieldName, New ScalarValue(p.First), Worm.Criteria.FilterOperation.GreaterEqualThan))
                    con.AddFilter(New cc.EntityFilter(original_type, fieldName, New ScalarValue(p.Second), Worm.Criteria.FilterOperation.LessEqualThan))
                    Dim bf As IFilter = con.Condition
                    'Dim f As IFilter = TryCast(bf, Worm.cc.IFilter)
                    'If f IsNot Nothing Then
                    sb.Append(bf.MakeQueryStmt(MappingEngine, SQLGenerator, GetFilterInfo, almgr, params, Nothing))
                    'Else
                    'sb.Append(bf.MakeSQLStmt(DbSchema, params))
                    'End If
                    If sb.Length > SQLGenerator.QueryLength Then
                        l.Add(New Pair(Of String, Integer)(" and (" & sb.ToString & ")", params.Params.Count))
                        sb.Length = 0
                    Else
                        sb.Append(" or ")
                    End If
                Next

                If mr.Rest.Count > 0 Then
                    Dim sb2 As New StringBuilder
                    sb2.Append("(")
                    For Each id As Integer In mr.Rest
                        sb2.Append(id).Append(",")

                        If sb2.Length > SQLGenerator.QueryLength - sb.Length Then
                            sb2.Length -= 1
                            sb2.Append(")")
                            Dim f As New cc.EntityFilter(original_type, fieldName, New LiteralValue(sb2.ToString), Worm.Criteria.FilterOperation.In)

                            sb.Append(f.MakeQueryStmt(MappingEngine, SQLGenerator, GetFilterInfo, almgr, params, Nothing))

                            sb.Insert(0, " and (")
                            l.Add(New Pair(Of String, Integer)(sb.ToString & ")", params.Params.Count))
                            sb.Length = 0
                            sb2.Length = 0
                            sb2.Append("(")
                        End If
                    Next
                    If sb2.Length <> 1 Then
                        sb2.Length -= 1
                        sb2.Append(")")
                        Dim f As New cc.EntityFilter(original_type, fieldName, New LiteralValue(sb2.ToString), Worm.Criteria.FilterOperation.In)
                        sb.Append(f.MakeQueryStmt(MappingEngine, SQLGenerator, GetFilterInfo, almgr, params, Nothing))

                        sb.Insert(0, " and (")
                        l.Add(New Pair(Of String, Integer)(sb.ToString & ")", params.Params.Count))
                        sb.Length = 0
                    End If
                End If

                If sb.Length > 0 Then
                    sb.Length -= 4
                    If sb.Length > 0 Then
                        l.Add(New Pair(Of String, Integer)(" and (" & sb.ToString & ")", params.Params.Count))
                    End If
                End If

            End If

            Return l
        End Function

        Protected Function GetFilters(ByVal ids_ As Generic.List(Of Object), ByVal table As SourceFragment, ByVal column As String, _
            ByVal almgr As AliasMgr, ByVal params As ParamMgr, ByVal idsSorted As Boolean) As Generic.IEnumerable(Of Pair(Of String, Integer))

            Dim mr As MergeResult = Nothing
            Dim l As New Generic.List(Of Pair(Of String, Integer))

            If ids_.Count > 0 Then
                Dim int As Integer
                Dim ids As Generic.List(Of Integer) = Nothing
                If Integer.TryParse(ids_(0).ToString, int) Then
                    ids = ids_.ConvertAll(Function(i) Convert.ToInt32(i))
                    mr = MergeIds(ids, Not idsSorted)
                Else
                    Dim sb As New StringBuilder
                    For Each o As Object In ids_
                        sb.Append(o.ToString).Append(",")
                    Next
                    sb.Length -= 1
                    Dim f As New cc.TableFilter(table, column, New LiteralValue(sb.ToString), Worm.Criteria.FilterOperation.In)
                    l.Add(New Pair(Of String, Integer)(f.MakeQueryStmt(MappingEngine, SQLGenerator, GetFilterInfo, almgr, params), params.Params.Count))
                End If
            End If

            If mr IsNot Nothing Then
                Dim sb As New StringBuilder

                For Each p As Pair(Of Integer) In mr.Pairs
                    Dim con As New Condition.ConditionConstructor
                    con.AddFilter(New cc.TableFilter(table, column, New ScalarValue(p.First), Worm.Criteria.FilterOperation.GreaterEqualThan))
                    con.AddFilter(New cc.TableFilter(table, column, New ScalarValue(p.Second), Worm.Criteria.FilterOperation.LessEqualThan))
                    Dim bf As IFilter = con.Condition
                    'Dim f As Worm.Database.Criteria.Core.IFilter = TryCast(bf, Worm.Database.Criteria.Core.IFilter)
                    'If f IsNot Nothing Then
                    sb.Append(bf.MakeQueryStmt(MappingEngine, SQLGenerator, GetFilterInfo, almgr, params, Nothing))
                    'Else
                    'sb.Append(bf.MakeSQLStmt(DbSchema, params))
                    'End If
                    If sb.Length > SQLGenerator.QueryLength Then
                        l.Add(New Pair(Of String, Integer)(" and (" & sb.ToString & ")", params.Params.Count))
                        sb.Length = 0
                    Else
                        sb.Append(" or ")
                    End If
                Next

                If mr.Rest.Count > 0 Then
                    Dim sb2 As New StringBuilder
                    sb2.Append("(")
                    For Each id As Integer In mr.Rest
                        sb2.Append(id).Append(",")

                        If sb2.Length > SQLGenerator.QueryLength - sb.Length Then
                            sb2.Length -= 1
                            sb2.Append(")")
                            Dim f As New cc.TableFilter(table, column, New LiteralValue(sb2.ToString), Worm.Criteria.FilterOperation.In)

                            sb.Append(f.MakeQueryStmt(MappingEngine, SQLGenerator, GetFilterInfo, almgr, params, Nothing))

                            sb.Insert(0, " and (")
                            l.Add(New Pair(Of String, Integer)(sb.ToString & ")", params.Params.Count))
                            sb.Length = 0
                            sb2.Length = 0
                            sb2.Append("(")
                        End If
                    Next
                    If sb2.Length <> 1 Then
                        sb2.Length -= 1
                        sb2.Append(")")
                        Dim f As New cc.TableFilter(table, column, New LiteralValue(sb2.ToString), Worm.Criteria.FilterOperation.In)
                        sb.Append(f.MakeQueryStmt(MappingEngine, SQLGenerator, GetFilterInfo, almgr, params))

                        sb.Insert(0, " and (")
                        l.Add(New Pair(Of String, Integer)(sb.ToString & ")", params.Params.Count))
                        sb.Length = 0
                    End If
                End If

                If sb.Length > 0 Then
                    sb.Length -= 4
                    If sb.Length > 0 Then
                        l.Add(New Pair(Of String, Integer)(" and (" & sb.ToString & ")", params.Params.Count))
                    End If
                End If

            End If

            Return l
        End Function

        Protected Overrides Function Search(Of T As {New, IKeyEntity})(ByVal type2search As Type, _
            ByVal contextKey As Object, ByVal sort As Sort, ByVal filter As IFilter, _
            ByVal frmt As IFtsStringFormater, Optional ByVal js() As QueryJoin = Nothing) As ReadOnlyList(Of T)

            Dim fields As New List(Of Pair(Of String, Type))
            Dim searchSchema As IEntitySchema = MappingEngine.GetObjectSchema(type2search)
            Dim selectType As System.Type = GetType(T)
            Dim selSchema As IEntitySchema = MappingEngine.GetObjectSchema(selectType)
            Dim fsearch As IOrmFullTextSupport = TryCast(searchSchema, IOrmFullTextSupport)
            Dim queryFields As String() = Nothing
            Dim selCols, searchCols As New List(Of ColumnAttribute)
            Dim ssearch As IOrmFullTextSupport = TryCast(selSchema, IOrmFullTextSupport)

            Dim joins As New List(Of QueryJoin)
            Dim appendMain As Boolean = PrepareSearch(selectType, type2search, filter, sort, contextKey, fields, _
                joins, selCols, searchCols, queryFields, searchSchema, selSchema)

            Dim col As New List(Of T)
            Using cmd As System.Data.Common.DbCommand = CreateDBCommand()

                With cmd
                    .CommandType = System.Data.CommandType.Text

                    Dim params As New ParamMgr(SQLGenerator, "p")
                    .CommandText = SQLGenerator.MakeSearchStatement(MappingEngine, type2search, selectType, frmt, fields, _
                        GetSearchSection, joins, SortType.Desc, params, GetFilterInfo, queryFields, _
                        Integer.MinValue, _
                        "containstable", sort, appendMain, CType(filter, IFilter), contextKey, _
                         selSchema, searchSchema)
                    params.AppendParams(.Parameters)
                End With

                If Not String.IsNullOrEmpty(cmd.CommandText) Then
                    If type2search Is selectType OrElse searchCols.Count = 0 Then
                        LoadMultipleObjects(Of T)(cmd, fields IsNot Nothing, col, selCols)
                    Else
                        LoadMultipleObjects(selectType, type2search, cmd, col, selCols, searchCols)
                    End If
                End If
            End Using

            Dim col2 As New List(Of T)
            Dim f2 As IOrmFullTextSupportEx = TryCast(searchSchema, IOrmFullTextSupportEx)
            Dim tokens() As String = frmt.GetTokens
            If tokens.Length > 1 AndAlso (f2 Is Nothing OrElse f2.UseFreeText) Then
                Using cmd As System.Data.Common.DbCommand = CreateDBCommand()
                    With cmd
                        .CommandType = System.Data.CommandType.Text

                        Dim params As New ParamMgr(SQLGenerator, "p")
                        .CommandText = SQLGenerator.MakeSearchStatement(MappingEngine, type2search, selectType, frmt, fields, _
                            GetSearchSection, joins, SortType.Desc, params, GetFilterInfo, queryFields, 500, _
                            "freetexttable", sort, appendMain, CType(filter, IFilter), _
                            contextKey, selSchema, searchSchema)
                        params.AppendParams(.Parameters)
                    End With

                    If Not String.IsNullOrEmpty(cmd.CommandText) Then
                        If type2search Is selectType OrElse searchCols.Count = 0 Then
                            LoadMultipleObjects(Of T)(cmd, fields IsNot Nothing, col2, selCols)
                        Else
                            LoadMultipleObjects(selectType, type2search, cmd, col2, selCols, searchCols)
                        End If
                    End If
                End Using
            End If

            Dim res As List(Of T) = Nothing
            If fields IsNot Nothing AndAlso selectType Is type2search AndAlso sort Is Nothing AndAlso col.Count > 0 Then
                Dim query As String, sb As New StringBuilder
                For Each tk As String In tokens
                    sb.Append(tk).Append(" ")
                Next
                sb.Length -= 1
                query = sb.ToString
                Dim full As New List(Of T)
                Dim full_part1 As New List(Of T)
                Dim full_part As New List(Of T)
                Dim starts As New List(Of T)
                Dim other As New List(Of T)
                For Each o As T In col
                    Dim str As Boolean = False
                    For Each p As Pair(Of String, Type) In fields
                        'If p.Second Is selectType Then
                        Dim f As String = p.First
                        Dim s As String = CStr(o.GetValueOptimized(Nothing, f, searchSchema))
                        If s IsNot Nothing Then
                            If s.Equals(query, StringComparison.InvariantCultureIgnoreCase) Then
                                full.Add(o)
                                GoTo l1
                            ElseIf s.Replace(".", "").Equals(query, StringComparison.InvariantCultureIgnoreCase) Then
                                full.Add(o)
                                GoTo l1
                            End If

                            Dim ss() As String = s.Split(New Char() {" "c, ","c})
                            For i As Integer = 0 To ss.Length - 1
                                Dim ps As String = ss(i)
                                If Not String.IsNullOrEmpty(ps) Then
                                    If ps.Equals(query, StringComparison.InvariantCultureIgnoreCase) Then
                                        If i = 0 Then
                                            full_part1.Add(o)
                                        Else
                                            full_part.Add(o)
                                        End If
                                        GoTo l1
                                    ElseIf ps.Replace(".", "").Equals(query, StringComparison.InvariantCultureIgnoreCase) Then
                                        If i = 0 Then
                                            full_part1.Add(o)
                                        Else
                                            full_part.Add(o)
                                        End If
                                        GoTo l1
                                    End If
                                End If
                            Next

                            If Not str AndAlso s.StartsWith(query, StringComparison.InvariantCultureIgnoreCase) Then
                                str = True
                            End If
                        End If
                        'End If
                    Next
                    If str Then
                        starts.Add(o)
                    Else
                        other.Add(o)
                    End If
l1:
                Next
                Dim cnt As Integer = full.Count
                _er = New ExecutionResult(cnt + full_part1.Count + full_part.Count + starts.Count + other.Count, Nothing, Nothing, False, 0)

                RaiseOnDataAvailable()

                Dim rf As Integer = Math.Max(0, _start - cnt)
                full.RemoveRange(0, Math.Min(_start, cnt))
                cnt = full.Count
                If cnt < _length Then
                    'If full_part1.Count + cnt < _length Then
                    '    full.AddRange(full_part1)
                    'Else
                    '    full_part1.RemoveRange(0, _length - cnt)
                    '    full.AddRange(full_part1)
                    '    GoTo l2
                    'End If
                    If AddPart(full, full_part1, cnt, rf) Then
                        If AddPart(full, full_part, cnt, rf) Then
                            If AddPart(full, starts, cnt, rf) Then
                                AddPart(full, other, cnt, rf)
                            End If
                        End If
                    End If
                    'full.AddRange(full_part)
                    'full.AddRange(starts)
                    'full.AddRange(other)
                Else
                    full.RemoveRange(0, _length)
                End If
l2:
                res = full
            Else
                _er = New ExecutionResult(col.Count, Nothing, Nothing, False, 0)
                RaiseOnDataAvailable()

                If _length = Integer.MaxValue AndAlso _start = 0 Then
                    res = New List(Of T)(col)
                Else
                    Dim l As IList(Of T) = CType(col, Global.System.Collections.Generic.IList(Of T))
                    res = New List(Of T)
                    For i As Integer = _start To Math.Min(_start + _length, col.Count) - 1
                        res.Add(l(i))
                    Next
                End If
            End If

            If col2 IsNot Nothing AndAlso res.Count < _length Then
                Dim dic As New Dictionary(Of Object, T)
                For Each o As T In res
                    dic.Add(o.Identifier, o)
                Next
                Dim i As Integer = res.Count
                For Each o As T In col2
                    If Not dic.ContainsKey(o.Identifier) Then
                        res.Add(o)
                        i += 1
                        If i = _start + _length Then
                            Exit For
                        End If
                    End If
                Next
                _er = New ExecutionResult(_er.Count + col2.Count, Nothing, Nothing, False, 0)
            End If

            Return New ReadOnlyList(Of T)(res)
        End Function

        Protected Overrides Function SearchEx(Of T As {IKeyEntity, New})(ByVal type2search As Type, _
            ByVal contextKey As Object, ByVal sort As Sort, ByVal filter As IFilter, ByVal ftsText As String, _
            ByVal limit As Integer, ByVal fts As IFtsStringFormater) As ReadOnlyList(Of T)

            Dim selectType As System.Type = GetType(T)
            Dim fields As New List(Of Pair(Of String, Type))
            Dim joins As New List(Of QueryJoin)
            Dim selCols, searchCols As New List(Of ColumnAttribute)
            Dim queryFields As String() = Nothing

            Dim searchSchema As IEntitySchema = MappingEngine.GetObjectSchema(type2search)
            Dim selSchema As IEntitySchema = MappingEngine.GetObjectSchema(selectType)

            Dim appendMain As Boolean = PrepareSearch(selectType, type2search, filter, sort, contextKey, fields, _
                joins, selCols, searchCols, queryFields, searchSchema, selSchema)

            Return New ReadOnlyList(Of T)(MakeSqlStmtSearch(Of T)(type2search, selectType, fields, queryFields, joins.ToArray, sort, appendMain, _
                filter, selCols, searchCols, ftsText, limit, fts, contextKey))
        End Function

        Public Function PrepareSearch(ByVal selectType As Type, ByVal type2search As Type, ByVal filter As IFilter, _
            ByVal sort As Sort, ByVal contextKey As Object, ByVal fields As IList(Of Pair(Of String, Type)), _
            ByVal joins As IList(Of QueryJoin), ByVal selCols As IList(Of ColumnAttribute), _
            ByVal searchCols As IList(Of ColumnAttribute), ByRef queryFields As String(), _
            ByVal searchSchema As IEntitySchema, ByVal selSchema As IEntitySchema) As Boolean

            'Dim searchSchema As IOrmObjectSchema = DbSchema.GetObjectSchema(type2search)
            'Dim selSchema As IOrmObjectSchema = DbSchema.GetObjectSchema(selectType)
            Dim fsearch As IOrmFullTextSupport = TryCast(searchSchema, IOrmFullTextSupport)
            'Dim queryFields As String() = Nothing
            Dim ssearch As IOrmFullTextSupport = TryCast(selSchema, IOrmFullTextSupport)
            'If ssearch IsNot Nothing Then
            '    Dim ss() As String = fsearch.GetIndexedFields
            '    If ss IsNot Nothing Then
            '        For Each s As String In ss
            '            fields.Add(New Pair(Of String, Type)(s, selectType))
            '            selCols.Add(New ColumnAttribute(s))
            '        Next
            '    End If
            '    If selCols.Count > 0 Then
            '        selCols.Insert(0, New ColumnAttribute("ID"))
            '        fields.Insert(0, New Pair(Of String, Type)("ID", selectType))
            '    End If
            'End If
            'If selectType IsNot type2search Then
            Dim pkname As String = MappingEngine.GetPrimaryKeys(selectType)(0).PropertyAlias
            selCols.Insert(0, New ColumnAttribute(pkname, Field2DbRelations.PK))
            fields.Insert(0, New Pair(Of String, Type)(pkname, selectType))
            'End If

            Dim types As New List(Of Type)

            If selectType IsNot type2search Then
                Dim field As String = MappingEngine.GetJoinFieldNameByType(selectType, type2search, selSchema)

                If String.IsNullOrEmpty(field) Then
                    field = MappingEngine.GetJoinFieldNameByType(type2search, selectType, searchSchema)

                    If String.IsNullOrEmpty(field) Then
                        Throw New OrmManagerException(String.Format("Relation {0} to {1} is ambiguous or not exist. Use FindJoin method", selectType, type2search))
                    End If

                    joins.Add(MakeJoin(selectType, type2search, field, Worm.Criteria.FilterOperation.Equal, JoinType.Join))
                Else
                    joins.Add(MakeJoin(type2search, selectType, field, Worm.Criteria.FilterOperation.Equal, JoinType.Join, True))
                End If

                types.Add(selectType)
            End If

            Dim appendMain As Boolean = False
            If sort IsNot Nothing Then
                For Each ns As Sort In New Sort.Iterator(sort)
                    Dim sortType As System.Type = ns.ObjectSource.GetRealType(MappingEngine)
                    appendMain = type2search Is sortType OrElse appendMain
                    If Not types.Contains(sortType) Then
                        If type2search IsNot sortType Then
                            Dim srtschema As IEntitySchema = MappingEngine.GetObjectSchema(sortType)
                            Dim field As String = MappingEngine.GetJoinFieldNameByType(type2search, sortType, searchSchema)
                            If Not String.IsNullOrEmpty(field) Then
                                joins.Add(MakeJoin(sortType, type2search, field, Worm.Criteria.FilterOperation.Equal, JoinType.Join))
                                types.Add(sortType)
                                Continue For
                            End If

                            'field = MappingEngine.GetJoinFieldNameByType(sortType, type2search, srtschema)
                            'If Not String.IsNullOrEmpty(field) Then
                            '    joins.Add(MakeJoin(type2search, sortType, field, FilterOperation.Equal, JoinType.Join, True))
                            '    Continue Do
                            'End If

                            If selectType IsNot type2search Then
                                'field = MappingEngine.GetJoinFieldNameByType(sortType, selectType, srtschema)
                                'If Not String.IsNullOrEmpty(field) Then
                                '    joins.Add(MakeJoin(sortType, selectType, field, FilterOperation.Equal, JoinType.Join))
                                '    Continue Do
                                'End If

                                field = MappingEngine.GetJoinFieldNameByType(selectType, sortType, selSchema)
                                If Not String.IsNullOrEmpty(field) Then
                                    joins.Add(MakeJoin(selectType, sortType, field, Worm.Criteria.FilterOperation.Equal, JoinType.Join, True))
                                    types.Add(sortType)
                                    Continue For
                                End If
                            End If

                            If String.IsNullOrEmpty(field) Then
                                Throw New OrmManagerException(String.Format("Relation {0} to {1} is ambiguous or not exist. Use FindJoin method", selectType, type2search))
                            End If
                        End If
                    End If
                Next
            End If

            If filter IsNot Nothing Then
                For Each f As IFilter In filter.GetAllFilters
                    Dim ef As IEntityFilter = TryCast(f, IEntityFilter)
                    If ef IsNot Nothing Then
                        Dim ot As OrmFilterTemplate = CType(ef.GetFilterTemplate, OrmFilterTemplate)
                        Dim type2join As System.Type = ot.ObjectSource.GetRealType(MappingEngine)

                        'If type2join Is Nothing AndAlso Not String.IsNullOrEmpty(ot.EntityName) Then
                        '    type2join = MappingEngine.GetTypeByEntityName(ot.EntityName)
                        'End If

                        If type2join Is Nothing Then
                            Throw New NullReferenceException("Type for OrmFilterTemplate must be specified")
                        End If

                        appendMain = type2search Is type2join OrElse appendMain
                        If type2search IsNot type2join Then
                            If Not types.Contains(type2join) Then
                                Dim field As String = MappingEngine.GetJoinFieldNameByType(type2search, type2join, searchSchema)

                                If Not String.IsNullOrEmpty(field) Then
                                    joins.Add(MakeJoin(type2join, type2search, field, Worm.Criteria.FilterOperation.Equal, JoinType.Join))
                                    types.Add(type2join)
                                Else
                                    If selectType IsNot type2search Then
                                        field = MappingEngine.GetJoinFieldNameByType(selectType, type2join, selSchema)
                                        If Not String.IsNullOrEmpty(field) Then
                                            joins.Add(MakeJoin(selectType, type2join, field, Worm.Criteria.FilterOperation.Equal, JoinType.Join, True))
                                            types.Add(type2join)
                                            Continue For
                                        End If
                                    End If

                                    Throw New OrmManagerException(String.Format("Relation {0} to {1} is ambiguous or not exist. Use FindJoin method", selectType, type2join))
                                End If
                            End If
                        End If
                    Else
                        Dim tf As cc.TableFilter = TryCast(f, cc.TableFilter)
                        If tf IsNot Nothing Then
                            If tf.Template.Table IsNot selSchema.Table Then
                                Throw New NotImplementedException
                            Else
                                appendMain = True
                            End If
                        Else
                            appendMain = True
                            'Throw New NotImplementedException
                        End If
                    End If
                Next
            End If

            If fsearch IsNot Nothing Then
                Dim ss() As String = fsearch.GetIndexedFields
                If ss IsNot Nothing Then
                    For Each s As String In ss
                        fields.Add(New Pair(Of String, Type)(s, type2search))
                        searchCols.Add(New ColumnAttribute(s))
                    Next
                End If

                If searchCols.Count > 0 Then
                    For Each c As ColumnAttribute In searchCols
                        selCols.Add(c)
                    Next
                    'searchCols.Insert(0, New ColumnAttribute("ID", Field2DbRelations.PK))
                    'fields.Insert(0, New Pair(Of String, Type)("ID", type2search))
                End If

                If contextKey IsNot Nothing Then
                    queryFields = fsearch.GetQueryFields(contextKey)
                End If
            End If

            Return appendMain
        End Function

        Public Function MakeSqlStmtSearch(Of T As {IKeyEntity, New})(ByVal type2search As Type, _
            ByVal selectType As Type, ByVal fields As ICollection(Of Pair(Of String, Type)), ByVal queryFields() As String, _
            ByVal joins() As QueryJoin, ByVal sort As Sort, ByVal appendMain As Boolean, ByVal filter As IFilter, _
            ByVal selCols As List(Of ColumnAttribute), ByVal searchCols As List(Of ColumnAttribute), _
            ByVal ftsText As String, ByVal limit As Integer, ByVal fts As IFtsStringFormater, ByVal contextkey As Object) As ReadOnlyList(Of T)

            Dim selSchema As IEntitySchema = MappingEngine.GetObjectSchema(selectType)
            Dim searchSchema As IEntitySchema = MappingEngine.GetObjectSchema(type2search)

            Using cmd As System.Data.Common.DbCommand = CreateDBCommand()

                With cmd
                    .CommandType = System.Data.CommandType.Text

                    Dim params As New ParamMgr(SQLGenerator, "p")
                    .CommandText = SQLGenerator.MakeSearchStatement(MappingEngine, type2search, selectType, fts, fields, _
                        GetSearchSection, joins, SortType.Desc, params, GetFilterInfo, queryFields, _
                        limit, ftsText, sort, appendMain, CType(filter, IFilter), contextkey, selSchema, searchSchema)
                    params.AppendParams(.Parameters)
                End With

                Dim r As New List(Of T)
                If type2search Is selectType OrElse searchCols.Count = 0 Then
                    LoadMultipleObjects(Of T)(cmd, fields IsNot Nothing, r, selCols)
                Else
                    LoadMultipleObjects(selectType, type2search, cmd, r, selCols, searchCols)
                End If
                Return New ReadOnlyList(Of T)(r)
            End Using

        End Function

        Private Function AddPart(Of T)(ByVal full As List(Of T), ByVal part As List(Of T), ByRef cnt As Integer, ByRef rf As Integer) As Boolean
            Dim r As Integer = rf
            Dim pcnt As Integer = part.Count
            rf = Math.Max(0, rf - pcnt)
            part.RemoveRange(0, Math.Min(r, pcnt))
            pcnt = part.Count
            If pcnt + cnt <= _length Then
                full.AddRange(part)
                cnt = full.Count
            Else
                part.RemoveRange(_length - cnt, pcnt - (_length - cnt))
                full.AddRange(part)
                Return False
            End If
            Return cnt < _length
        End Function

        Protected Overrides Function BuildDictionary(Of T As {New, IKeyEntity})(ByVal level As Integer, ByVal filter As IFilter, ByVal joins() As QueryJoin) As DicIndex(Of T)
            Invariant()
            Dim params As New ParamMgr(SQLGenerator, "p")
            Using cmd As System.Data.Common.DbCommand = CreateDBCommand()
                cmd.CommandText = SQLGenerator.GetDictionarySelect(MappingEngine, GetType(T), level, params, CType(filter, IFilter), joins, GetFilterInfo)
                cmd.CommandType = System.Data.CommandType.Text
                params.AppendParams(cmd.Parameters)
                Dim b As ConnAction = TestConn(cmd)
                Try
                    Dim root As DicIndex(Of T) = BuildDictionaryInternal(Of T)(cmd, level, Me, Nothing, Nothing)
                    root.Filter = filter
                    root.Join = joins
                    Return root
                Finally
                    CloseConn(b)
                End Try
            End Using
        End Function

        Protected Overrides Function BuildDictionary(Of T As {New, IKeyEntity})(ByVal level As Integer, _
            ByVal filter As IFilter, ByVal joins() As QueryJoin, ByVal firstField As String, ByVal secondField As String) As DicIndex(Of T)
            Invariant()
            Dim params As New ParamMgr(SQLGenerator, "p")
            Using cmd As System.Data.Common.DbCommand = CreateDBCommand()
                cmd.CommandText = SQLGenerator.GetDictionarySelect(MappingEngine, GetType(T), level, params, CType(filter, IFilter), joins, GetFilterInfo, firstField, secondField)
                cmd.CommandType = System.Data.CommandType.Text
                params.AppendParams(cmd.Parameters)
                Dim b As ConnAction = TestConn(cmd)
                Try
                    Dim root As DicIndex(Of T) = BuildDictionaryInternal(Of T)(cmd, level, Me, firstField, secondField)
                    root.Filter = filter
                    root.Join = joins
                    Return root
                Finally
                    CloseConn(b)
                End Try
            End Using
        End Function

        Protected Shared Function BuildDictionaryInternal(Of T As {New, IKeyEntity})(ByVal cmd As System.Data.Common.DbCommand, ByVal level As Integer, ByVal mgr As OrmReadOnlyDBManager, _
            ByVal firstField As String, ByVal secField As String) As DicIndex(Of T)
            Dim last As DicIndex(Of T) = New DicIndex(Of T)("ROOT", Nothing, 0, firstField, secField)
            Dim root As DicIndex(Of T) = last
            'Dim arr As New Hashtable
            'Dim arr1 As New ArrayList
            Dim first As Boolean = True
            Dim et As New PerfCounter
            Using dr As System.Data.IDataReader = cmd.ExecuteReader
                If mgr IsNot Nothing Then
                    mgr._exec = et.GetTime
                End If
                Dim ft As New PerfCounter
                Do While dr.Read

                    Dim name As String = dr.GetString(0)
                    Dim cnt As Integer = dr.GetInt32(1)

                    BuildDic(Of T)(name, cnt, level, root, last, first, firstField, secField)
                Loop
                If mgr IsNot Nothing Then
                    mgr._fetch = ft.GetTime
                End If
            End Using

            'Dim tt As Type = GetType(MediaIndex(Of T))
            'Return CType(arr1.ToArray(tt), MediaIndex(Of T)())
            Return root
        End Function

        Public Overrides Function UpdateObject(ByVal obj As _ICachedEntity) As Boolean
            Throw New NotImplementedException()
        End Function

        'Public Overrides Function AddObject(ByVal obj As OrmBase) As OrmBase
        '    Throw New NotImplementedException()
        'End Function

        Protected Overrides Function InsertObject(ByVal obj As _ICachedEntity) As Boolean
            Throw New NotImplementedException()
        End Function

        Protected Friend Overrides Sub DeleteObject(ByVal obj As ICachedEntity)
            Throw New NotImplementedException()
        End Sub

        'Protected Overrides Sub Obj2ObjRelationSave2(ByVal obj As OrmBase, ByVal dt As System.Data.DataTable, ByVal sync As String, ByVal t As System.Type)
        '    Throw New NotImplementedException()
        'End Sub

        Protected Overrides Sub M2MSave(ByVal obj As Entities.IKeyEntity, ByVal t As System.Type, ByVal key As String, ByVal el As EditableListBase)
            Throw New NotImplementedException
        End Sub

        'Public Overrides Function SaveChanges(ByVal obj As OrmBase, ByVal AcceptChanges As Boolean) As Boolean
        '    Throw New NotImplementedException()
        'End Function

        Protected Overrides Function GetSearchSection() As String
            Return String.Empty
        End Function

        Protected Friend Overrides ReadOnly Property Exec() As System.TimeSpan
            Get
                Return _exec
            End Get
        End Property

        Protected Friend Overrides ReadOnly Property Fecth() As System.TimeSpan
            Get
                Return _fetch
            End Get
        End Property

        Protected Friend Function LoadM2M(Of T As {IKeyEntity, New})(ByVal cmd As System.Data.Common.DbCommand, ByVal withLoad As Boolean, _
            ByVal obj As IKeyEntity, ByVal sort As Sort, ByVal columns As IList(Of ColumnAttribute)) As List(Of Object)
            Dim b As ConnAction = TestConn(cmd)
            Dim tt As Type = GetType(T)
            Dim c As OrmCache = TryCast(_cache, OrmCache)
            Try
                If withLoad AndAlso c IsNot Nothing Then
                    c.BeginTrackDelete(tt)
                End If
                _loadedInLastFetch = 0
                Dim dic As IDictionary = CType(GetDictionary(Of T)(), System.Collections.IDictionary)
                Dim oschema As IEntitySchema = MappingEngine.GetObjectSchema(tt)
                Dim props As IDictionary = MappingEngine.GetProperties(tt, oschema)
                Dim cm As Collections.IndexedCollection(Of String, MapField2Column) = oschema.GetFieldColumnMap
                Dim l As New List(Of Object)

                Dim et As New PerfCounter
                Using dr As System.Data.Common.DbDataReader = cmd.ExecuteReader
                    _exec = et.GetTime
                    Dim ft As New PerfCounter
                    Do While dr.Read
                        Dim id1 As Object = dr.GetValue(0)
                        If Not id1.Equals(obj.Identifier) Then
                            Throw New OrmManagerException("Wrong relation statement")
                        End If
                        Dim id2 As Object = dr.GetValue(1)
                        l.Add(id2)

                        Dim o_ As T = CreateOrmBase(Of T)(id2)
                        o_.SetObjectState(ObjectState.NotLoaded)
                        Dim o As T = CType(NormalizeObject(o_, CType(dic, System.Collections.IDictionary)), T)
                        'If _raiseCreated AndAlso ReferenceEquals(o, o_) Then
                        '    RaiseObjectLoaded(o)
                        'End If
                        RaiseObjectLoaded(o)

                        If withLoad AndAlso (c Is Nothing OrElse Not c.IsDeleted(tt, o.Key)) Then
                            If o.ObjectState <> ObjectState.Modified Then
                                Using o.GetSyncRoot()
                                    'If obj.IsLoaded Then obj.IsLoaded = False
                                    Dim lock As IDisposable = Nothing
                                    Try
                                        Dim ro As _IEntity = LoadFromDataReader(o, dr, columns, False, 2, dic, True, lock, oschema, cm, props)
                                        AfterLoadingProcess(dic, o, lock, ro)
                                    Finally
                                        If lock IsNot Nothing Then
                                            'Threading.Monitor.Exit(dic)
                                            lock.Dispose()
                                        End If
                                    End Try
                                    'ro.CorrectStateAfterLoading(Object.ReferenceEquals(ro, o))
                                    _loadedInLastFetch += 1
                                End Using
                            End If
                        End If
                    Loop
                    _fetch = ft.GetTime
                End Using

                If sort IsNot Nothing AndAlso sort.IsExternal Then
                    Dim l2 As New List(Of Object)
                    For Each o As T In MappingEngine.ExternalSort(Of T)(Me, sort, ConvertIds2Objects(Of T)(l, False).List)
                        l2.Add(o.Identifier)
                    Next
                    l = l2
                End If
                Return l
            Finally
                If withLoad AndAlso c IsNot Nothing Then
                    c.EndTrackDelete(tt)
                End If
                CloseConn(b)
            End Try
        End Function

        Public Overrides Function GetObjectFromStorage(ByVal obj As Entities._ICachedEntity) As Entities.ICachedEntity
            Invariant()

            If obj Is Nothing Then
                Throw New ArgumentNullException("obj")
            End If

            Dim original_type As Type = obj.GetType

            'Dim filter As New cc.EntityFilter(original_type, "ID", _
            '    New EntityValue(obj), Worm.Criteria.FilterOperation.Equal)
            Dim c As New Condition.ConditionConstructor '= Database.Criteria.Conditions.Condition.ConditionConstructor
            For Each p As PKDesc In obj.GetPKValues
                c.AddFilter(New cc.EntityFilter(original_type, p.PropertyAlias, New ScalarValue(p.Value), Worm.Criteria.FilterOperation.Equal))
            Next
            Dim filter As IFilter = c.Condition

            Using cmd As System.Data.Common.DbCommand = CreateDBCommand()
                Dim arr As Generic.List(Of ColumnAttribute) = MappingEngine.GetSortedFieldList(original_type)

                With cmd
                    .CommandType = System.Data.CommandType.Text

                    Dim almgr As AliasMgr = AliasMgr.Create
                    Dim params As New ParamMgr(SQLGenerator, "p")
                    Dim sb As New StringBuilder
                    sb.Append(SQLGenerator.Select(MappingEngine, original_type, almgr, params, arr, Nothing, GetFilterInfo))
                    SQLGenerator.AppendWhere(MappingEngine, original_type, filter, almgr, sb, GetFilterInfo, params)

                    params.AppendParams(.Parameters)
                    .CommandText = sb.ToString
                End With

                Dim newObj As _ICachedEntity = CreateObject(obj.GetPKValues, original_type)
                newObj.SetObjectState(ObjectState.NotLoaded)
                Dim b As ConnAction = TestConn(cmd)
                Try
                    LoadSingleObject(cmd, arr, newObj, False, True, False, Nothing)
                    newObj.SetObjectState(ObjectState.Clone)
                    Return newObj
                Finally
                    CloseConn(b)
                End Try
            End Using
        End Function
    End Class
End Namespace
