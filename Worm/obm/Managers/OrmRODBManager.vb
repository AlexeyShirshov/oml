Imports Worm
Imports System.Collections.Generic
Imports CoreFramework.Structures
Imports CoreFramework.Threading

Namespace Orm
    Partial Public Class OrmReadOnlyDBManager
        Inherits OrmManagerBase

        Protected Friend Enum ConnAction
            Leave
            Destroy
            Close
        End Enum

#Region " Classes "

        Public Class BatchSaver
            Implements IDisposable

            Private _disposedValue As Boolean
            Private _l As New List(Of OrmBase)
            Private _mgr As OrmReadOnlyDBManager
            Private _acceptInBatch As Boolean
            Private _callbacks As OrmCache.IUpdateCacheCallbacks
            Private _save As Nullable(Of Boolean)
            Private _disposeMgr As Boolean

            Public Event BeginSave(ByVal count As Integer)
            Public Event ObjectSaved(ByVal o As OrmBase)
            Public Event ObjectAccepted(ByVal o As OrmBase)
            Public Event ObjectRejected(ByVal o As OrmBase)
            Public Event ObjectRestored(ByVal o As OrmBase)
            Public Event SaveSuccessed()
            Public Event SaveFailed()
            Public Event BeginRejecting()
            Public Event BeginAccepting()

            Public Sub New(ByVal mgr As OrmReadOnlyDBManager, ByVal dispose As Boolean)
                _mgr = mgr
                _disposeMgr = dispose
            End Sub

            Public Sub New(ByVal mgr As OrmReadOnlyDBManager)
                _mgr = mgr
            End Sub

            Public Sub New()
                _mgr = CType(OrmManagerBase.CurrentManager, OrmReadOnlyDBManager)
            End Sub

            Public ReadOnly Property Manager() As OrmReadOnlyDBManager
                Get
                    Return _mgr
                End Get
            End Property

            Public Property IsCommit() As Boolean
                Get
                    If _save.HasValue Then
                        Return _save.Value
                    End If
                    Return False
                End Get
                Friend Set(ByVal value As Boolean)
                    _save = value
                End Set
            End Property

            Public Sub Commit()
                _save = True
            End Sub

            Public Sub Rollback()
                _save = False
            End Sub

            Public Property AcceptInBatch() As Boolean
                Get
                    Return _acceptInBatch
                End Get
                Set(ByVal value As Boolean)
                    _acceptInBatch = value
                End Set
            End Property

            Public Property CacheCallbacks() As OrmCache.IUpdateCacheCallbacks
                Get
                    Return _callbacks
                End Get
                Set(ByVal value As OrmCache.IUpdateCacheCallbacks)
                    _callbacks = value
                End Set
            End Property

            Public Sub Add(ByVal o As OrmBase)
                _l.Add(o)
            End Sub

            Public Sub AddRange(ByVal col As ICollection(Of OrmBase))
                _l.AddRange(col)
            End Sub

            Protected Sub Save()
                Dim hasTransaction As Boolean = _mgr.Transaction IsNot Nothing
                Dim [error] As Boolean = True
                Dim saved As New List(Of OrmBase), copies As New List(Of Pair(Of OrmBase))
                Dim rejectList As New List(Of OrmBase), need2save As New List(Of OrmBase)

                _mgr.BeginTransaction()
                Try
                    RaiseEvent BeginSave(_l.Count)
                    For Each o As OrmBase In _l
                        If o.ObjectState = ObjectState.Created Then
                            rejectList.Add(o)
                        ElseIf o.ObjectState = ObjectState.Modified Then
                            copies.Add(New Pair(Of OrmBase)(o, o.GetFullClone))
                        End If
                        Try
                            If o.Save(False) Then
                                need2save.Add(o)
                            Else
                                RaiseEvent ObjectSaved(o)
                            End If
                            saved.Add(o)
                        Catch ex As Exception
                            Throw New OrmManagerException("Error during save " & o.ObjName, ex)
                        End Try
                    Next

                    For Each o As OrmBase In need2save
                        If o.Save(False) Then
                            Throw New OrmManagerException(String.Format("It seems {0} has relation(s) to new objects after second save", o.ObjName))
                        End If
                        RaiseEvent ObjectSaved(o)
                    Next

                    [error] = False
                Finally
                    If [error] Then
                        If Not hasTransaction Then
                            _mgr.Rollback()
                        End If

                        RaiseEvent BeginRejecting()
                        Rollback(saved, rejectList, copies)
                        RaiseEvent SaveFailed()
                    Else
                        If Not hasTransaction Then
                            _mgr.Commit()
                        End If

                        RaiseEvent BeginAccepting()
                        If _acceptInBatch Then
                            Dim l As New Dictionary(Of OrmBase, OrmBase)
                            Dim l2 As New Dictionary(Of Type, List(Of OrmBase))
                            For Each o As OrmBase In saved
                                Dim mo As OrmBase = o.AcceptChanges(False)
                                l.Add(o, mo)
                                Dim ls As List(Of OrmBase) = Nothing
                                If Not l2.TryGetValue(o.GetType, ls) Then
                                    ls = New List(Of OrmBase)
                                    l2.Add(o.GetType, ls)
                                End If
                                ls.Add(o)
                                RaiseEvent ObjectAccepted(o)
                            Next
                            For Each t As Type In l2.Keys
                                Dim ls As List(Of OrmBase) = l2(t)
                                _mgr.Cache.UpdateCache(_mgr.ObjectSchema, ls, _mgr, _
                                    AddressOf OrmBase.Accept_AfterUpdateCache, l, _callbacks)
                            Next
                        Else
                            For Each o As OrmBase In saved
                                o.AcceptChanges(True)
                                RaiseEvent ObjectAccepted(o)
                            Next
                        End If

                        RaiseEvent SaveSuccessed()
                    End If
                End Try
            End Sub

            Private Sub Rollback(ByVal saved As List(Of OrmBase), ByVal rejectList As List(Of OrmBase), ByVal copies As List(Of Pair(Of OrmBase)))
                For Each o As OrmBase In rejectList
                    o.RejectChanges()
                    RaiseEvent ObjectRejected(o)
                Next
                For Each o As Pair(Of OrmBase) In copies
                    o.First.CopyBodyInternal(o.Second, o.First)
                    o.First.ObjectState = o.Second._old_state
                    RaiseEvent ObjectRestored(o.First)
                Next
                For Each o As OrmBase In saved
                    If Not rejectList.Contains(o) Then
                        o.RejectRelationChanges()
                        RaiseEvent ObjectRejected(o)
                    End If
                Next
            End Sub

#Region " IDisposable Support "
            Protected Overridable Sub Dispose(ByVal disposing As Boolean)
                If Not Me._disposedValue Then
                    If Not _save.HasValue Then
                        Throw New InvalidOperationException("You should commit or rollback Saver")
                    End If
                    If disposing AndAlso _save.Value Then
                        Save()
                    End If
                    If _disposeMgr Then
                        _mgr.Dispose()
                    End If
                End If
                Me._disposedValue = True
            End Sub

            Public Sub Dispose() Implements IDisposable.Dispose
                Dispose(True)
                GC.SuppressFinalize(Me)
            End Sub
#End Region

        End Class

        Public Class OrmTransactionalScope
            Implements IDisposable

            Private disposedValue As Boolean
            Private _disposing As Boolean
            Private _objs As New List(Of OrmBase)
            Private _mgr As OrmManagerBase
            Private _saver As BatchSaver

            Public Event SaveComplete()

            Public Sub New()
                MyClass.New(CType(OrmManagerBase.CurrentManager, OrmReadOnlyDBManager))
            End Sub

            Public Sub New(ByVal mgr As OrmReadOnlyDBManager)
                If mgr Is Nothing Then
                    Throw New ArgumentNullException("mgr")
                End If

                AddHandler mgr.BeginUpdate, AddressOf Add
                AddHandler mgr.BeginDelete, AddressOf Delete
                _saver = New BatchSaver(mgr)
                _mgr = mgr
            End Sub

            Public ReadOnly Property IsCommit() As Boolean
                Get
                    Return _saver.IsCommit
                End Get
            End Property

            Public Sub Commit()
                _saver.Commit()
            End Sub

            Public Sub Rollback()
                _saver.Rollback()
            End Sub

            Public ReadOnly Property Saver() As BatchSaver
                Get
                    Return _saver
                End Get
            End Property

            Public Overridable Sub AddRange(ByVal objs As ICollection(Of OrmBase))
                If objs Is Nothing Then
                    Throw New ArgumentNullException("objects")
                End If

                If _disposing Then
                    Throw New InvalidOperationException("Cannot add object during save")
                End If

                _objs.AddRange(objs)
                _saver.AddRange(objs)
            End Sub

            Public Overridable Sub Add(ByVal obj As OrmBase)
                If obj Is Nothing Then
                    Throw New ArgumentNullException("object")
                End If

                If _disposing Then
                    Throw New InvalidOperationException("Cannot add object during save")
                End If

                _objs.Add(obj)
                _saver.Add(obj)
            End Sub

            Protected Sub Delete(ByVal obj As OrmBase)
                If obj Is Nothing Then
                    Throw New ArgumentNullException("object")
                End If

                If _disposing Then
                    Throw New InvalidOperationException("Cannot add object during save")
                End If

                If Not _objs.Contains(obj) Then
                    _objs.Add(obj)
                    _saver.Add(obj)
                End If
            End Sub

            Public Function CreateNewObject(Of T As {OrmBase, New})() As T
                If _mgr.NewObjectManager Is Nothing Then
                    Throw New InvalidOperationException("NewObjectManager is not set")
                End If

                Return CreateNewObject(Of T)(_mgr.NewObjectManager.GetIdentity)
            End Function

            Public Overridable Function CreateNewObject(Of T As {OrmBase, New})(ByVal id As Integer) As T
                If _mgr.NewObjectManager Is Nothing Then
                    Throw New InvalidOperationException("NewObjectManager is not set")
                End If
                Dim o As New T
                o.Init(id, _mgr.Cache, _mgr.ObjectSchema)
                _mgr.NewObjectManager.AddNew(o)
                '_objs.Add(o)
                '_saver.Add(o)
                Add(o)
                Return o
            End Function

            Public Function CreateNewObject(ByVal t As Type) As OrmBase
                If _mgr.NewObjectManager Is Nothing Then
                    Throw New InvalidOperationException("NewObjectManager is not set")
                End If

                Return CreateNewObject(t, _mgr.NewObjectManager.GetIdentity)
            End Function

            Public Function CreateNewObject(ByVal t As Type, ByVal id As Integer) As OrmBase
                For Each mi As Reflection.MethodInfo In Me.GetType.GetMember("CreateNewObject", Reflection.MemberTypes.Method, _
                    Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public)
                    If mi.IsGenericMethod Then
                        mi = mi.MakeGenericMethod(New Type() {t})
                        Return CType(mi.Invoke(Me, New Object() {id}), OrmBase)
                    End If
                Next
                Throw New InvalidOperationException("Cannot find method CreateNewObject")
            End Function

            Protected Sub _Rollback()
                For Each o As OrmBase In _objs
                    If o.ObjectState = ObjectState.Created Then
                        If _mgr.NewObjectManager IsNot Nothing Then
                            _mgr.NewObjectManager.RemoveNew(o)
                        End If
                    Else
                        o.RejectChanges()
                    End If
                Next
            End Sub

#Region " IDisposable Support "
            ' IDisposable
            Protected Overridable Sub Dispose(ByVal disposing As Boolean)
                If Not Me.disposedValue Then
                    If _saver.IsCommit Then
                        If disposing Then
                            Dim err As Boolean = True
                            Try
                                _disposing = True
                                _saver.Dispose()
                                err = False
                            Finally
                                _disposing = False
                                If err Then
                                    _Rollback()
                                End If
                            End Try
                        End If
                    Else
                        _Rollback()
                    End If
                    RemoveHandler _mgr.BeginDelete, AddressOf Delete
                    RemoveHandler _mgr.BeginUpdate, AddressOf Add

                    If _saver.IsCommit Then
                        RaiseEvent SaveComplete()
                    End If
                End If
                Me.disposedValue = True
            End Sub

            ' This code added by Visual Basic to correctly implement the disposable pattern.
            Public Sub Dispose() Implements IDisposable.Dispose
                ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
                Dispose(True)
                GC.SuppressFinalize(Me)
            End Sub
#End Region

        End Class

#End Region

        Declare Function QueryPerformanceCounter Lib "Kernel32" (ByRef X As Long) As Short
        Declare Function QueryPerformanceFrequency Lib "Kernel32" (ByRef X As Long) As Short

        Private _connStr As String
        Private _tran As System.Data.Common.DbTransaction
        Private _closeConnOnCommit As ConnAction
        Private _conn As System.Data.Common.DbConnection
        Private _exec As TimeSpan
        Private _fetch As TimeSpan

        Class PerfCounter
            Private _start As Long
            Private _end As Long

            Public Sub New()
                QueryPerformanceCounter(_start)
            End Sub

            Public Function GetTime() As TimeSpan
                QueryPerformanceCounter(_end)
                Dim f As Long
                QueryPerformanceFrequency(f)
                Return TimeSpan.FromSeconds((_end - _start) / f)
            End Function
        End Class

        Protected Shared _LoadMultipleObjectsMI As Reflection.MethodInfo = Nothing

        Public Sub New(ByVal cache As OrmCacheBase, ByVal schema As DbSchema, ByVal connectionString As String)
            MyBase.New(cache, schema)
            _connStr = connectionString
        End Sub

        Protected Sub New(ByVal schema As DbSchema, ByVal connectionString As String)
            MyBase.New(schema)

            _connStr = connectionString
        End Sub

        Public ReadOnly Property DbSchema() As DbSchema
            Get
                Return CType(_schema, DbSchema)
            End Get
        End Property

        Protected Function CreateConn() As System.Data.Common.DbConnection
            Return DbSchema.CreateConnection(_connStr)
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

        Protected Overloads Overrides Function GetCustDelegate(Of T As {New, OrmBase})(ByVal relation As M2MRelation, ByVal filter As IFilter, _
            ByVal sort As Sort, ByVal key As String, ByVal id As String) As OrmManagerBase.ICustDelegate(Of T)
            Return New DistinctRelationFilterCustDelegate(Of T)(Me, relation, filter, sort, key, id)
        End Function

        Protected Overloads Overrides Function GetCustDelegate(Of T As {New, OrmBase})(ByVal aspect As QueryAspect, ByVal join() As OrmJoin, ByVal filter As IFilter, _
            ByVal sort As Sort, ByVal key As String, ByVal id As String) As OrmManagerBase.ICustDelegate(Of T)
            Return New JoinCustDelegate(Of T)(Me, join, filter, sort, key, id, aspect)
        End Function

        Protected Overloads Overrides Function GetCustDelegate(Of T As {New, OrmBase})(ByVal filter As IFilter, _
            ByVal sort As Sort, ByVal key As String, ByVal id As String) As OrmManagerBase.ICustDelegate(Of T)
            Return New FilterCustDelegate(Of T)(Me, filter, sort, key, id)
        End Function

        Protected Overloads Overrides Function GetCustDelegate(Of T As {New, OrmBase})(ByVal filter As IFilter, _
            ByVal sort As Sort, ByVal key As String, ByVal id As String, ByVal cols() As String) As OrmManagerBase.ICustDelegate(Of T)
            If cols Is Nothing Then
                Throw New ArgumentNullException("cols")
            End If
            Dim l As New List(Of ColumnAttribute)
            Dim has_id As Boolean = False
            For Each c As String In cols
                Dim col As ColumnAttribute = DbSchema.GetColumnByFieldName(GetType(T), c)
                If col Is Nothing Then
                    Throw New ArgumentException("Invalid column name " & c)
                End If
                If c = "ID" Then
                    has_id = True
                End If
                l.Add(col)
            Next
            If Not has_id Then
                l.Add(DbSchema.GetColumnByFieldName(GetType(T), "ID"))
            End If
            Return New FilterCustDelegate(Of T)(Me, filter, l, sort, key, id)
        End Function

        'Protected Overrides Function GetCustDelegate4Top(Of T As {New, OrmBase})(ByVal top As Integer, ByVal filter As IOrmFilter, _
        '    ByVal sort As Sort, ByVal key As String, ByVal id As String) As OrmManagerBase.ICustDelegate(Of T)
        '    Return New FilterCustDelegate4Top(Of T)(Me, top, filter, sort, key, id)
        'End Function

        Protected Overloads Overrides Function GetCustDelegate(Of T2 As {New, OrmBase})( _
            ByVal obj As OrmBase, ByVal filter As IFilter, ByVal sort As Sort, _
            ByVal id As String, ByVal key As String, ByVal direct As Boolean) As OrmManagerBase.ICustDelegate(Of T2)
            Return New M2MDataProvider(Of T2)(Me, obj, filter, sort, id, key, direct)
        End Function

        'Protected Overrides Function GetCustDelegateTag(Of T As {New, OrmBase})( _
        '    ByVal obj As T, ByVal filter As IOrmFilter, ByVal sort As String, ByVal sortType As SortType, _
        '    ByVal id As String, ByVal sync As String, ByVal key As String) As OrmManagerBase.ICustDelegate(Of T)
        '    '    Return New M2MCustDelegate(Of T)(Me, obj, filter, sort, sortType, id, sync, key, True)
        '    Throw New NotImplementedException
        'End Function

        Protected Function FindConnected(ByVal ct As Type, ByVal selectedType As Type, _
            ByVal filterType As Type, ByVal connectedFilter As IFilter, _
            ByVal filter As IFilter, ByVal withLoad As Boolean, _
            ByVal sort As Sort) As IList
            Using cmd As System.Data.Common.DbCommand = DbSchema.CreateDBCommand
                Dim arr As Generic.List(Of ColumnAttribute) = Nothing

                With cmd
                    .CommandType = System.Data.CommandType.Text

                    Dim almgr As AliasMgr = AliasMgr.Create
                    Dim params As New ParamMgr(DbSchema, "p")
                    Dim schema2 As IOrmObjectSchema = DbSchema.GetObjectSchema(selectedType)
                    'Dim schema1 As IOrmObjectSchema = Schema.GetObjectSchema(filterType)
                    'Dim r1 As M2MRelation = Schema.GetM2MRelation(selectedType, filterType)
                    Dim r2 As M2MRelation = DbSchema.GetM2MRelation(filterType, selectedType, True)
                    Dim id_clm As String = r2.Column

                    Dim sb As New StringBuilder
                    arr = ObjectSchema.GetSortedFieldList(ct)
                    If withLoad Then
                        'Dim jc As New OrmFilter(ct, field, selectedType, "ID", FilterOperation.Equal)
                        'Dim j As New OrmJoin(Schema.GetTables(selectedType)(0), JoinType.Join, jc)
                        'Dim js As New List(Of OrmJoin)
                        'js.Add(j)
                        'js.AddRange(Schema.GetAllJoins(selectedType))
                        Dim columns As String = DbSchema.GetSelectColumnList(selectedType)
                        sb.Append(DbSchema.Select(ct, almgr, params, arr, columns))
                    Else
                        sb.Append(DbSchema.Select(ct, almgr, params, arr))
                    End If
                    'If withLoad Then
                    '    arr = DatabaseSchema.GetSortedFieldList(ct)
                    '    sb.Append(Schema.Select(ct, almgr, params, arr))
                    'Else
                    '    arr = New Generic.List(Of ColumnAttribute)
                    '    arr.Add(New ColumnAttribute("ID", Field2DbRelations.PK))
                    '    sb.Append(Schema.SelectID(ct, almgr, params))
                    'End If
                    Dim appendMainTable As Boolean = filter IsNot Nothing OrElse schema2.GetFilter(GetFilterInfo) IsNot Nothing OrElse withLoad OrElse (sort IsNot Nothing AndAlso Not sort.IsExternal) OrElse DbSchema.NeedJoin(schema2)
                    'Dim table As String = schema2.GetTables(0)
                    DbSchema.AppendJoins(selectedType, almgr, schema2.GetTables, sb, params, DbSchema.GetObjectSchema(ct).GetTables(0), id_clm, appendMainTable)
                    If withLoad Then
                        For Each tbl As OrmTable In schema2.GetTables
                            If almgr.Aliases.ContainsKey(tbl) Then
                                Dim [alias] As String = almgr.Aliases(tbl)
                                sb = sb.Replace(tbl.TableName & ".", [alias] & ".")
                            End If
                        Next
                    End If
                    Dim con As New Orm.Condition.ConditionConstructor
                    con.AddFilter(connectedFilter)
                    con.AddFilter(filter)
                    con.AddFilter(schema2.GetFilter(GetFilterInfo))
                    DbSchema.AppendWhere(ct, con.Condition, almgr, sb, GetFilterInfo, params)

                    If sort IsNot Nothing AndAlso Not sort.IsExternal Then
                        DbSchema.AppendOrder(selectedType, sort, almgr, sb)
                    End If

                    params.AppendParams(.Parameters)
                    .CommandText = sb.ToString
                End With

                If withLoad Then
                    Return LoadMultipleObjects(ct, selectedType, cmd, Nothing, Nothing)
                Else
                    Return LoadMultipleObjects(ct, cmd, True, arr)
                End If
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

        Protected Function LoadMultipleObjects(ByVal firstType As Type, _
            ByVal secondType As Type, ByVal cmd As System.Data.Common.DbCommand, _
            ByVal first_cols As List(Of ColumnAttribute), ByVal sec_cols As List(Of ColumnAttribute)) As IList

            Dim values As IList
            values = CType(GetType(List(Of )).MakeGenericType(New Type() {firstType}).InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, Nothing, Nothing), System.Collections.IList)
            If first_cols Is Nothing Then
                first_cols = DbSchema.GetSortedFieldList(firstType)
            End If

            If sec_cols Is Nothing Then
                sec_cols = DbSchema.GetSortedFieldList(secondType)
            End If

            Dim b As ConnAction = TestConn(cmd)
            Try
                Dim et As New PerfCounter
                Using dr As System.Data.IDataReader = cmd.ExecuteReader
                    _exec = et.GetTime
                    Dim firstidx As Integer = 0
                    Dim pk_name As String = _schema.GetPrimaryKeysName(firstType, False)(0)
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
                    Dim pk2_name As String = _schema.GetPrimaryKeysName(secondType, False)(0)
                    Try
                        secidx = dr.GetOrdinal(pk2_name)
                    Catch ex As IndexOutOfRangeException
                        If _mcSwitch.TraceError Then
                            Trace.WriteLine("Invalid column name " & pk_name & " in " & cmd.CommandText)
                            Trace.WriteLine(Environment.StackTrace)
                        End If
                        Throw New OrmManagerException("Cannot get second primary key ordinal", ex)
                    End Try

                    Dim ft As New PerfCounter
                    Do While dr.Read
                        Dim id1 As Integer = CInt(dr.GetValue(firstidx))
                        Dim id2 As Integer = CInt(dr.GetValue(secidx))
                        Dim obj1 As OrmBase = CreateDBObject(id1, firstType)
                        Dim obj2 As OrmBase = CreateDBObject(id2, secondType)
                        If obj1.ObjectState <> ObjectState.Modified Then
                            Using obj1.GetSyncRoot()
                                If obj1.IsLoaded Then obj1.IsLoaded = False
                                LoadFromDataReader(obj1, dr, first_cols, False)
                                If obj1.ObjectState = ObjectState.NotLoaded Then obj1.ObjectState = ObjectState.None
                                values.Add(obj1)
                            End Using
                        Else
                            values.Add(obj1)
                        End If

                        If obj2.ObjectState <> ObjectState.Modified Then
                            Using obj2.GetSyncRoot()
                                If obj2.IsLoaded Then obj2.IsLoaded = False
                                LoadFromDataReader(obj2, dr, sec_cols, False, first_cols.Count)
                                If obj2.ObjectState = ObjectState.NotLoaded Then obj2.ObjectState = ObjectState.None
                            End Using
                        End If
                    Loop
                    _fetch = ft.GetTime

                    Return values
                End Using
            Finally
                CloseConn(b)
            End Try

        End Function

        Protected Function LoadMultipleObjects(ByVal t As Type, _
            ByVal cmd As System.Data.Common.DbCommand, _
            ByVal withLoad As Boolean, _
            ByVal arr As Generic.List(Of ColumnAttribute)) As IList
            'Dim ltg As Type = GetType(IList(Of ))
            'Dim lt As Type = ltg.MakeGenericType(New Type() {t})
            Dim flags As Reflection.BindingFlags = Reflection.BindingFlags.NonPublic Or Reflection.BindingFlags.Instance

            If _LoadMultipleObjectsMI Is Nothing Then
                For Each mi2 As Reflection.MethodInfo In Me.GetType.GetMethods(flags)
                    If mi2.Name = "LoadMultipleObjects" And mi2.IsGenericMethod Then
                        _LoadMultipleObjectsMI = mi2
                        Exit For
                    End If
                Next

                If _LoadMultipleObjectsMI Is Nothing Then
                    Throw New OrmManagerException("Cannot find method LoadMultipleObjects")
                End If
            End If

            Dim mi_real As Reflection.MethodInfo = _LoadMultipleObjectsMI.MakeGenericMethod(New Type() {t})

            Return CType(mi_real.Invoke(Me, flags, Nothing, _
                New Object() {cmd, withLoad, Nothing, arr}, Nothing), IList)

        End Function

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

        Protected Overrides Function GetObjects(Of T As {OrmBase, New})(ByVal type As Type, ByVal ids As Generic.IList(Of Integer), ByVal f As IFilter, _
            ByVal relation As M2MRelation, ByVal idsSorted As Boolean, ByVal withLoad As Boolean) As IDictionary(Of Integer, EditableList)
            Invariant()

            If ids Is Nothing Then
                Throw New ArgumentNullException("ids")
            End If

            If ids.Count < 1 Then
                Return Nothing
            End If

            Dim almgr As AliasMgr = AliasMgr.Create
            Dim params As New ParamMgr(DbSchema, "p")
            'Dim arr As Generic.List(Of ColumnAttribute) = Nothing
            Dim sb As New StringBuilder
            Dim type2load As Type = GetType(T)
            Dim ct As Type = DbSchema.GetConnectedType(type, type2load)
            Dim direct As Boolean = Not relation.non_direct

            'Dim dt As New System.Data.DataTable()
            'dt.TableName = "table1"
            'dt.Locale = System.Globalization.CultureInfo.CurrentCulture

            Dim edic As New Dictionary(Of Integer, EditableList)

            If ct IsNot Nothing Then
                'If Not direct Then
                '    Throw New NotSupportedException("Tag is not supported with connected type")
                'End If

                'Dim oschema2 As IOrmObjectSchema = DbSchema.GetObjectSchema(type2load)
                'Dim r2 As M2MRelation = DbSchema.GetM2MRelation(type2load, type, direct)
                Dim f1 As String = DbSchema.GetConnectedTypeField(ct, type, Not direct)
                Dim f2 As String = DbSchema.GetConnectedTypeField(ct, type2load, direct)
                'Dim col1 As String = type.Name & "ID"
                'Dim col2 As String = orig_type.Name & "ID"
                'dt.Columns.Add(col1, GetType(Integer))
                'dt.Columns.Add(col2, GetType(Integer))

                For Each o As OrmBase In GetObjects(ct, ids, f, withLoad, f1, idsSorted)
                    'Dim o1 As OrmBase = CType(DbSchema.GetFieldValue(o, f1), OrmBase)
                    'Dim o2 As OrmBase = CType(DbSchema.GetFieldValue(o, f2), OrmBase)
                    Dim o1 As OrmBase = CType(o.GetValue(f1), OrmBase)
                    Dim o2 As OrmBase = CType(o.GetValue(f2), OrmBase)

                    Dim id1 As Integer = o1.Identifier
                    Dim id2 As Integer = o2.Identifier
                    'Dim k As Integer = o1.Identifier
                    'Dim v As Integer = o2.Identifier
                    If o2.GetType Is type Then
                        id1 = o2.Identifier
                        id2 = o1.Identifier
                    End If

                    Dim el As EditableList = Nothing
                    If edic.TryGetValue(id1, el) Then
                        el.Add(id2)
                    Else
                        Dim l As New List(Of Integer)
                        l.Add(id2)
                        el = New EditableList(id1, l, type, type2load, Nothing)
                        edic.Add(id1, el)
                    End If
                Next
            Else
                Dim oschema2 As IOrmObjectSchema = DbSchema.GetObjectSchema(type2load)
                Dim r2 As M2MRelation = DbSchema.GetM2MRelation(type2load, type, direct)
                Dim appendMainTable As Boolean = f IsNot Nothing OrElse oschema2.GetFilter(GetFilterInfo) IsNot Nothing
                sb.Append(DbSchema.SelectM2M(type2load, type, appendMainTable, True, params, almgr, withLoad, direct))

                If Not DbSchema.AppendWhere(type2load, f, almgr, sb, GetFilterInfo, params) Then
                    sb.Append(" where 1=1 ")
                End If

                Dim pcnt As Integer = params.Params.Count
                Dim nidx As Integer = pcnt
                For Each cmd_str As Pair(Of String, Integer) In GetFilters(CType(ids, List(Of Integer)), relation.Table, r2.Column, almgr, params, idsSorted)
                    Dim sb_cmd As New StringBuilder
                    sb_cmd.Append(sb.ToString).Append(cmd_str.First)
                    'Dim msort As Boolean = False
                    'If Not String.IsNullOrEmpty(sort) AndAlso Schema.GetObjectSchema(original_type).IsExternalSort(sort) Then
                    '    msort = True
                    'Else
                    '    Schema.AppendOrder(original_type, sort, sortType, almgr, sb_cmd)
                    'End If

                    Using cmd As System.Data.Common.DbCommand = DbSchema.CreateDBCommand
                        With cmd
                            .CommandType = System.Data.CommandType.Text
                            .CommandText = sb_cmd.ToString
                            params.AppendParams(.Parameters, 0, pcnt)
                            params.AppendParams(.Parameters, nidx, cmd_str.Second - nidx)
                            nidx = cmd_str.Second
                        End With
                        Dim arr As Generic.IList(Of ColumnAttribute) = Nothing
                        If withLoad Then
                            arr = DbSchema.GetSortedFieldList(type2load)
                        End If
                        Dim b As ConnAction = TestConn(cmd)
                        Try
                            Dim et As New PerfCounter
                            Using dr As System.Data.IDataReader = cmd.ExecuteReader
                                _exec = et.GetTime

                                Dim ft As New PerfCounter
                                Do While dr.Read
                                    Dim id1 As Integer = CInt(dr.GetValue(0))
                                    Dim id2 As Integer = CInt(dr.GetValue(1))
                                    Dim el As EditableList = Nothing
                                    If edic.TryGetValue(id1, el) Then
                                        el.Add(id2)
                                    Else
                                        Dim l As New List(Of Integer)
                                        l.Add(id2)
                                        el = New EditableList(id1, l, type, type2load, Nothing)
                                        edic.Add(id1, el)
                                    End If
                                    If withLoad Then
                                        Dim obj As T = CreateDBObject(Of T)(id2)
                                        If obj.ObjectState <> ObjectState.Modified Then
                                            Using obj.GetSyncRoot()
                                                If obj.IsLoaded Then obj.IsLoaded = False
                                                LoadFromDataReader(obj, dr, arr, False, 2)
                                                If obj.ObjectState = ObjectState.NotLoaded Then obj.ObjectState = ObjectState.None
                                            End Using
                                        End If
                                    End If
                                Loop
                                _fetch = ft.GetTime
                            End Using
                        Finally
                            CloseConn(b)
                        End Try
                    End Using
                Next
            End If
            Return edic
        End Function

        Protected Overloads Function GetObjects(ByVal ct As Type, ByVal ids As Generic.IList(Of Integer), _
            ByVal f As IFilter, ByVal withLoad As Boolean, ByVal fieldName As String, ByVal idsSorted As Boolean) As IList
            If ids Is Nothing Then
                Throw New ArgumentNullException("ids")
            End If

            Dim objs As New ArrayList
            If ids.Count < 1 Then
                Return objs
            End If

            Dim original_type As Type = ct
            Dim almgr As AliasMgr = AliasMgr.Create
            Dim params As New ParamMgr(DbSchema, "p")
            Dim arr As Generic.List(Of ColumnAttribute) = Nothing
            Dim sb As New StringBuilder
            If withLoad Then
                arr = _schema.GetSortedFieldList(original_type)
                sb.Append(DbSchema.Select(original_type, almgr, params, arr))
            Else
                arr = New Generic.List(Of ColumnAttribute)
                arr.Add(New ColumnAttribute("ID", Field2DbRelations.PK))
                sb.Append(DbSchema.SelectID(original_type, almgr, params))
            End If

            If Not DbSchema.AppendWhere(original_type, f, almgr, sb, GetFilterInfo, params) Then
                sb.Append(" where 1=1 ")
            End If

            Dim pcnt As Integer = params.Params.Count
            Dim nidx As Integer = pcnt
            For Each cmd_str As Pair(Of String, Integer) In GetFilters(CType(ids, List(Of Integer)), fieldName, almgr, params, original_type, idsSorted)
                Dim sb_cmd As New StringBuilder
                sb_cmd.Append(sb.ToString).Append(cmd_str.First)
                'Dim msort As Boolean = False
                'If Not String.IsNullOrEmpty(sort) AndAlso Schema.GetObjectSchema(original_type).IsExternalSort(sort) Then
                '    msort = True
                'Else
                '    Schema.AppendOrder(original_type, sort, sortType, almgr, sb_cmd)
                'End If

                Using cmd As System.Data.Common.DbCommand = DbSchema.CreateDBCommand
                    With cmd
                        .CommandType = System.Data.CommandType.Text
                        .CommandText = sb_cmd.ToString
                        params.AppendParams(.Parameters, 0, pcnt)
                        params.AppendParams(.Parameters, nidx, cmd_str.Second - nidx)
                        nidx = cmd_str.Second
                    End With
                    objs.AddRange(LoadMultipleObjects(original_type, cmd, withLoad, arr))
                    'If msort Then
                    '    objs = Schema.GetObjectSchema(original_type).ExternalSort(sort, sortType, objs)
                    'End If
                End Using

                'params.Clear(pcnt)
            Next
            Return objs
        End Function

        Protected Overrides Function GetObjects(Of T As {OrmBase, New})(ByVal ids As Generic.IList(Of Integer), ByVal f As IFilter, ByVal objs As IList(Of T), _
            ByVal withLoad As Boolean, ByVal fieldName As String, ByVal idsSorted As Boolean) As Generic.IList(Of T)
            Invariant()

            If ids Is Nothing Then
                Throw New ArgumentNullException("ids")
            End If

            If ids.Count < 1 Then
                Return objs
            End If

            Dim original_type As Type = GetType(T)
            Dim almgr As AliasMgr = AliasMgr.Create
            Dim params As New ParamMgr(DbSchema, "p")
            Dim arr As Generic.List(Of ColumnAttribute) = Nothing
            Dim sb As New StringBuilder
            If withLoad Then
                arr = _schema.GetSortedFieldList(original_type)
                sb.Append(DbSchema.Select(original_type, almgr, params, arr))
            Else
                arr = New Generic.List(Of ColumnAttribute)
                arr.Add(New ColumnAttribute("ID", Field2DbRelations.PK))
                sb.Append(DbSchema.SelectID(original_type, almgr, params))
            End If

            If Not DbSchema.AppendWhere(original_type, f, almgr, sb, GetFilterInfo, params) Then
                sb.Append(" where 1=1 ")
            End If

            Dim pcnt As Integer = params.Params.Count
            Dim nidx As Integer = pcnt
            For Each cmd_str As Pair(Of String, Integer) In GetFilters(CType(ids, List(Of Integer)), fieldName, almgr, params, original_type, idsSorted)
                Dim sb_cmd As New StringBuilder
                sb_cmd.Append(sb.ToString).Append(cmd_str.First)
                'Dim msort As Boolean = False
                'If Not String.IsNullOrEmpty(sort) AndAlso Schema.GetObjectSchema(original_type).IsExternalSort(sort) Then
                '    msort = True
                'Else
                '    Schema.AppendOrder(original_type, sort, sortType, almgr, sb_cmd)
                'End If

                Using cmd As System.Data.Common.DbCommand = DbSchema.CreateDBCommand
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

        'Protected Overridable Function GetNewObject(ByVal type As Type, ByVal id As Integer) As OrmBase
        '    Dim o As OrmBase = Nothing
        '    If  IsNot Nothing Then
        '        o = FindNewDelegate(type, id)
        '    End If
        '    Return o
        'End Function

        Protected Friend Overrides Sub LoadObject(ByVal obj As OrmBase)
            Invariant()

            If obj Is Nothing Then
                Throw New ArgumentNullException("obj")
            End If

            Dim original_type As Type = obj.GetType

            Dim filter As New EntityFilter(original_type, "ID", New EntityValue(obj), FilterOperation.Equal)

            Using cmd As System.Data.Common.DbCommand = DbSchema.CreateDBCommand
                Dim arr As Generic.List(Of ColumnAttribute) = _schema.GetSortedFieldList(original_type)

                With cmd
                    .CommandType = System.Data.CommandType.Text

                    Dim almgr As AliasMgr = AliasMgr.Create
                    Dim params As New ParamMgr(DbSchema, "p")
                    Dim sb As New StringBuilder
                    sb.Append(DbSchema.Select(original_type, almgr, params, Nothing))
                    DbSchema.AppendWhere(original_type, filter, almgr, sb, GetFilterInfo, params)

                    params.AppendParams(.Parameters)
                    .CommandText = sb.ToString
                End With

                Dim olds As ObjectState = obj.ObjectState
                LoadSingleObject(cmd, arr, obj, True, True, False)
                'If olds = obj.ObjectState OrElse obj.ObjectState = ObjectState.Created Then obj.ObjectState = ObjectState.None
                '(olds = ObjectState.Created AndAlso obj.ObjectState = ObjectState.Modified) OrElse
                If olds = obj.ObjectState Then
                    obj.ObjectState = ObjectState.None
                End If
            End Using
        End Sub

        Protected Sub LoadSingleObject(ByVal cmd As System.Data.Common.DbCommand, _
            ByVal arr As Generic.IList(Of ColumnAttribute), ByVal obj As OrmBase, _
            ByVal check_pk As Boolean, ByVal load As Boolean, ByVal modifiedloaded As Boolean)
            Invariant()

            Dim b As ConnAction = TestConn(cmd)
            Try
                'Debug.WriteLine(cmd.CommandText)
                Dim et As New PerfCounter
                Using dr As System.Data.IDataReader = cmd.ExecuteReader
                    Using obj.GetSyncRoot()
                        Dim old As Boolean = obj.IsLoaded
                        If Not modifiedloaded Then obj.IsLoaded = False
                        'obj.IsLoaded = False
                        Dim loaded As Boolean = False
                        Do While dr.Read
                            If loaded Then
                                Throw New OrmManagerException(String.Format("Statement [{0}] returns more than one record", cmd.CommandText))
                            End If
                            LoadFromDataReader(obj, dr, arr, check_pk)
                            loaded = True
                        Loop
                        If Not obj.IsLoaded Then
                            If load Then
                                obj.ObjectState = ObjectState.NotFoundInDB
                                RemoveObjectFromCache(obj)
                            Else
                                If dr.RecordsAffected = 0 Then
                                    Throw DbSchema.PrepareConcurrencyException(obj)
                                Else
                                    obj.IsLoaded = old
                                End If
                            End If
                        Else
                            If obj.ObjectState = ObjectState.Created Then
                                'obj.ObjectState = ObjectState.None
                                obj._loading = True
                                obj.Identifier = obj.Identifier
                                obj._loading = False
                            End If
                        End If
                    End Using
                    _cache.LogLoadTime(obj, et.GetTime)
                End Using
            Catch
                Throw
            Finally
                CloseConn(b)
            End Try

        End Sub

        Protected Friend Function LoadMultipleObjects(Of T As {OrmBase, New})( _
            ByVal cmd As System.Data.Common.DbCommand, _
            ByVal withLoad As Boolean, ByVal values As Generic.IList(Of T), _
            ByVal arr As Generic.List(Of ColumnAttribute)) As Generic.IList(Of T)
            Invariant()

            'Dim idx As Integer = -1
            Dim b As ConnAction = TestConn(cmd)
            Dim original_type As Type = GetType(T)
            Try
                _loadedInLastFetch = 0
                Dim et As New PerfCounter
                Using dr As System.Data.IDataReader = cmd.ExecuteReader
                    _exec = et.GetTime
                    If values Is Nothing Then
                        values = New Generic.List(Of T)
                    End If
                    'If idx = -1 Then
                    '    Dim pk_name As String = _schema.GetPrimaryKeysName(original_type, False)(0)
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

                    Dim idx As Integer = GetPrimaryKeyIdx(cmd.CommandText, original_type, dr)
                    Dim dic As Generic.IDictionary(Of Integer, T) = GetDictionary(Of T)()
                    Dim ft As New PerfCounter
                    Do While dr.Read
                        LoadFromResultSet(Of T)(withLoad, CType(values, System.Collections.IList), arr, dr, idx, dic, _loadedInLastFetch)
                    Loop
                    _fetch = ft.GetTime

                    Return values
                End Using
            Finally
                CloseConn(b)
            End Try
        End Function

        Protected Function GetPrimaryKeyIdx(ByVal cmdtext As String, ByVal original_type As Type, ByVal dr As System.Data.IDataReader) As Integer
            Dim idx As Integer = -1
            Dim pk_name As String = _schema.GetPrimaryKeysName(original_type, False)(0)
            Try
                idx = dr.GetOrdinal(pk_name)
            Catch ex As IndexOutOfRangeException
                If _mcSwitch.TraceError Then
                    Trace.WriteLine("Invalid column name " & pk_name & " in " & cmdtext)
                    Trace.WriteLine(Environment.StackTrace)
                End If
                Throw New OrmManagerException("Cannot get primary key ordinal", ex)
            End Try
            Return idx
        End Function

        Protected Friend Sub LoadFromResultSet(Of T As {OrmBase, New})( _
            ByVal withLoad As Boolean, _
            ByVal values As IList, ByVal arr As Generic.List(Of ColumnAttribute), _
            ByVal dr As System.Data.IDataReader, ByVal idx As Integer, _
            ByVal dic As IDictionary(Of Integer, T), ByRef loaded As Integer)

            Dim id As Integer = CInt(dr.GetValue(idx))
            Dim obj As OrmBase = CreateDBObject(Of T)(id, dic, withLoad OrElse Not ListConverter.IsWeak)
            If obj IsNot Nothing Then
                If withLoad AndAlso obj.ObjectState <> ObjectState.Modified Then
                    Using obj.GetSyncRoot()
                        If obj.IsLoaded Then obj.IsLoaded = False
                        LoadFromDataReader(obj, dr, arr, False)
                        'If Not obj.IsLoaded Then
                        '    obj.ObjectState = ObjectState.NotFoundInDB
                        '    RemoveObjectFromCache(obj)
                        'Else
                        If obj.ObjectState = ObjectState.NotLoaded Then obj.ObjectState = ObjectState.None
                        values.Add(obj)
                        loaded += 1
                        'End If
                    End Using
                Else
                    values.Add(obj)
                    If obj.IsLoaded Then
                        loaded += 1
                    End If
                End If
            Else
                If _mcSwitch.TraceVerbose Then
                    WriteLine("Attempt to load unallowed object " & GetType(T).Name & " (" & id & ")")
                End If
            End If
        End Sub

        Protected Sub LoadFromDataReader(ByVal obj As OrmBase, ByVal dr As System.Data.IDataReader, _
            ByVal arr As Generic.IList(Of ColumnAttribute), ByVal check_pk As Boolean, Optional ByVal displacement As Integer = 0)

            Dim original_type As Type = obj.GetType
            Dim oschema As IOrmObjectSchema = DbSchema.GetObjectSchema(original_type)
            Dim fields_idx As Collections.IndexedCollection(Of String, MapField2Column) = oschema.GetFieldColumnMap
            Using obj.GetSyncRoot()
                obj._loading = True
                Dim has_pk As Boolean = False
                Dim pi_cache(arr.Count - 1) As Reflection.PropertyInfo
                Dim idic As IDictionary = _schema.GetProperties(original_type)
                For idx As Integer = 0 To arr.Count - 1
                    Dim c As ColumnAttribute = arr(idx)
                    Dim pi As Reflection.PropertyInfo = CType(idic(c), Reflection.PropertyInfo)
                    pi_cache(idx) = pi
                    If pi IsNot Nothing Then
                        If idx >= 0 AndAlso (fields_idx(c.FieldName).GetAttributes(c) And Field2DbRelations.PK) = Field2DbRelations.PK Then
                            Assert(idx + displacement < dr.FieldCount, c.FieldName)
                            If dr.FieldCount <= idx + displacement Then
                                If _mcSwitch.TraceError Then
                                    Dim dt As System.Data.DataTable = dr.GetSchemaTable
                                    Dim sb As New StringBuilder
                                    For Each drow As System.Data.DataRow In dt.Rows
                                        If sb.Length > 0 Then
                                            sb.Append(", ")
                                        End If
                                        sb.Append(drow("ColumnName")).Append("(").Append(drow("ColumnOrdinal")).Append(")")
                                    Next
                                    WriteLine(sb.ToString)
                                End If
                            End If
                            has_pk = True
                            Dim value As Object = dr.GetValue(idx + displacement)
                            If Not dr.IsDBNull(idx + displacement) Then
                                Try
                                    If (pi.PropertyType Is GetType(Boolean) AndAlso value.GetType Is GetType(Short)) OrElse (pi.PropertyType Is GetType(Integer) AndAlso value.GetType Is GetType(Long)) Then
                                        Dim v As Object = Convert.ChangeType(value, pi.PropertyType)
                                        obj.SetValue(pi, c, v)
                                        obj.SetLoaded(c, True)
                                    ElseIf pi.PropertyType Is GetType(Byte()) AndAlso value.GetType Is GetType(Date) Then
                                        Dim dt As DateTime = CDate(value)
                                        Dim l As Long = dt.ToBinary
                                        Using ms As New IO.MemoryStream
                                            Dim sw As New IO.StreamWriter(ms)
                                            sw.Write(l)
                                            sw.Flush()
                                            obj.SetValue(pi, c, ms.ToArray)
                                            obj.SetLoaded(c, True)
                                        End Using
                                    Else
                                        If c.FieldName = "ID" Then
                                            obj.Identifier = CInt(value)
                                        Else
                                            obj.SetValue(pi, c, value)
                                        End If
                                        obj.SetLoaded(c, True)
                                    End If
                                Catch ex As ArgumentException When ex.Message.StartsWith("Object of type 'System.DateTime' cannot be converted to type 'System.Byte[]'")
                                    Dim dt As DateTime = CDate(value)
                                    Dim l As Long = dt.ToBinary
                                    Using ms As New IO.MemoryStream
                                        Dim sw As New IO.StreamWriter(ms)
                                        sw.Write(l)
                                        sw.Flush()
                                        obj.SetValue(pi, c, ms.ToArray)
                                        obj.SetLoaded(c, True)
                                    End Using
                                Catch ex As ArgumentException When ex.Message.IndexOf("cannot be converted") > 0
                                    Dim v As Object = Convert.ChangeType(value, pi.PropertyType)
                                    obj.SetValue(pi, c, v)
                                    obj.SetLoaded(c, True)
                                End Try
                            End If
                        End If
                    End If
                Next

                If obj.ObjectState = ObjectState.Created Then
                    If has_pk Then
                        Throw New OrmManagerException("PK is not loaded")
                    Else
                        obj.CreateModified(obj.Identifier)
                    End If
                End If

                For idx As Integer = 0 To arr.Count - 1
                    Dim c As ColumnAttribute = arr(idx)
                    Dim pi As Reflection.PropertyInfo = pi_cache(idx) '_schema.GetProperty(original_type, c)

                    Dim value As Object = dr.GetValue(idx + displacement)

                    If pi Is Nothing Then
                        obj.SetValue(pi, c, value)
                        obj.SetLoaded(c, True, False)
                    Else
                        Dim att As Field2DbRelations = fields_idx(c.FieldName).GetAttributes(c)
                        If check_pk AndAlso (att And Field2DbRelations.PK) = Field2DbRelations.PK Then
                            Dim v As Object = pi.GetValue(obj, Nothing)
                            If Not value.GetType Is pi.PropertyType Then
                                value = Convert.ChangeType(value, pi.PropertyType)
                            End If
                            If Not v.Equals(value) Then
                                Throw New OrmManagerException("PK values is not equals (" & dr.GetName(idx + displacement) & "): value from db: " & value.ToString & "; value from object: " & v.ToString)
                            End If
                        ElseIf Not dr.IsDBNull(idx + displacement) AndAlso (att And Field2DbRelations.PK) <> Field2DbRelations.PK Then
                            If (att And Field2DbRelations.Factory) = Field2DbRelations.Factory Then
                                obj.CreateObject(c.FieldName, value)
                                obj.SetLoaded(c, True)
                                'If GetType(OrmBase) Is pi.PropertyType Then
                                '    obj.CreateObject(CInt(value))
                                '    obj.SetLoaded(c, True)
                                'Else
                                '    Dim type_created As Type = pi.PropertyType
                                '    Dim o As OrmBase = CreateDBObject(CInt(value), type_created)
                                '    obj.SetValue(pi, c, o)
                                '    obj.SetLoaded(c, True)
                                'End If
                            ElseIf GetType(OrmBase).IsAssignableFrom(pi.PropertyType) Then
                                Dim type_created As Type = pi.PropertyType
                                Dim o As OrmBase = CreateDBObject(CInt(value), type_created)
                                obj.SetValue(pi, c, o)
                                obj.SetLoaded(c, True)
                            ElseIf GetType(System.Xml.XmlDocument) Is pi.PropertyType AndAlso TypeOf (value) Is String Then
                                Dim o As New System.Xml.XmlDocument
                                o.LoadXml(CStr(value))
                                obj.SetValue(pi, c, o)
                                obj.SetLoaded(c, True)
                            ElseIf pi.PropertyType.IsEnum AndAlso TypeOf (value) Is String Then
                                Dim svalue As String = CStr(value).Trim
                                If svalue = String.Empty Then
                                    obj.SetValue(pi, c, 0)
                                    obj.SetLoaded(c, True)
                                Else
                                    obj.SetValue(pi, c, [Enum].Parse(pi.PropertyType, svalue, True))
                                    obj.SetLoaded(c, True)
                                End If
                            ElseIf pi.PropertyType.IsGenericType AndAlso GetType(Nullable(Of )).Name = pi.PropertyType.Name Then
                                Dim t As Type = pi.PropertyType.GetGenericArguments()(0)
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
                                Dim v2 As Object = pi.PropertyType.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, _
                                    Nothing, Nothing, New Object() {v})
                                obj.SetValue(pi, c, v2)
                                obj.SetLoaded(c, True)
                            Else
                                Try
                                    If (pi.PropertyType.IsPrimitive AndAlso value.GetType.IsPrimitive) OrElse (pi.PropertyType Is GetType(Long) AndAlso value.GetType Is GetType(Decimal)) Then
                                        Dim v As Object = Convert.ChangeType(value, pi.PropertyType)
                                        obj.SetValue(pi, c, v)
                                        obj.SetLoaded(c, True)
                                    ElseIf pi.PropertyType Is GetType(Byte()) AndAlso value.GetType Is GetType(Date) Then
                                        Dim dt As DateTime = CDate(value)
                                        Dim l As Long = dt.ToBinary
                                        Using ms As New IO.MemoryStream
                                            Dim sw As New IO.StreamWriter(ms)
                                            sw.Write(l)
                                            sw.Flush()
                                            'pi.SetValue(obj, ms.ToArray, Nothing)
                                            obj.SetValue(pi, c, ms.ToArray)
                                            obj.SetLoaded(c, True)
                                        End Using
                                        'ElseIf pi.PropertyType Is GetType(ReleaseDate) AndAlso value.GetType Is GetType(Integer) Then
                                        '    obj.SetValue(pi, c, pi.PropertyType.InvokeMember(Nothing, Reflection.BindingFlags.CreateInstance, Nothing, _
                                        '        Nothing, New Object() {value}))
                                        '    obj.SetLoaded(c, True)
                                    Else
                                        obj.SetValue(pi, c, value)
                                        obj.SetLoaded(c, True)
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
                                    Dim v As Object = Convert.ChangeType(value, pi.PropertyType)
                                    obj.SetValue(pi, c, v)
                                    obj.SetLoaded(c, True)
                                End Try
                            End If
                        ElseIf dr.IsDBNull(idx + displacement) Then
                            obj.SetValue(pi, c, Nothing)
                            obj.SetLoaded(c, True)
                        End If
                    End If
                Next
                obj._loading = False
                obj.CheckIsAllLoaded()
            End Using
        End Sub

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

        Protected Friend Overrides Function LoadObjectsInternal(Of T As {OrmBase, New})( _
            ByVal objs As Generic.ICollection(Of T), ByVal start As Integer, ByVal length As Integer, _
            ByVal remove_not_found As Boolean, ByVal columns As Generic.List(Of ColumnAttribute)) As Generic.ICollection(Of T)

            Invariant()

            If objs.Count < 1 Then
                Return objs
            End If

            If start > objs.Count Then
                Throw New ArgumentException(String.Format("The range {0},{1} is greater than array length: " & objs.Count, start, length))
            End If

            length = Math.Min(length, objs.Count - start)

            Dim ids As Generic.List(Of Integer) = FormPKValues(Of T)(Me, objs, start, length, True, columns)
            If ids.Count < 1 Then
                Return objs
            End If

            Dim original_type As Type = GetType(T)
            Dim almgr As AliasMgr = AliasMgr.Create
            Dim params As New ParamMgr(DbSchema, "p")
            Dim sb As New StringBuilder
            sb.Append(DbSchema.Select(original_type, almgr, params, columns))
            If Not DbSchema.AppendWhere(original_type, Nothing, almgr, sb, GetFilterInfo, params) Then
                sb.Append(" where 1=1 ")
            End If
            Dim values As New Generic.List(Of T)
            Dim pcnt As Integer = params.Params.Count
            Dim nextp As Integer = pcnt
            For Each cmd_str As Pair(Of String, Integer) In GetFilters(ids, "ID", almgr, params, original_type, True)
                Dim sb_cmd As New StringBuilder
                sb_cmd.Append(sb.ToString).Append(cmd_str.First)

                Using cmd As System.Data.Common.DbCommand = DbSchema.CreateDBCommand
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

            values.Clear()
            For Each o As T In objs
                If o.IsLoaded Then
                    values.Add(o)
                End If
            Next
            Return values

        End Function

        Protected Friend Overrides Function LoadObjectsInternal(Of T As {OrmBase, New})( _
            ByVal objs As Generic.ICollection(Of T), ByVal start As Integer, ByVal length As Integer, _
            ByVal remove_not_found As Boolean) As Generic.ICollection(Of T)
            Invariant()

            If objs.Count < 1 Then
                Return objs
            End If

            If start > objs.Count Then
                Throw New ArgumentException(String.Format("The range {0},{1} is greater than array length: " & objs.Count, start, length))
            End If

            length = Math.Min(length, objs.Count - start)

            Dim ids As Generic.List(Of Integer) = FormPKValues(Of T)(Me, objs, start, length)
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
            Dim params As New ParamMgr(DbSchema, "p")
            Dim columns As Generic.List(Of ColumnAttribute) = _schema.GetSortedFieldList(original_type)
            Dim sb As New StringBuilder
            sb.Append(DbSchema.Select(original_type, almgr, params, columns))
            If Not DbSchema.AppendWhere(original_type, Nothing, almgr, sb, GetFilterInfo, params) Then
                sb.Append(" where 1=1 ")
            End If
            Dim values As New Generic.List(Of T)
            Dim pcnt As Integer = params.Params.Count
            Dim nextp As Integer = pcnt
            For Each cmd_str As Pair(Of String, Integer) In GetFilters(ids, "ID", almgr, params, original_type, True)
                Dim sb_cmd As New StringBuilder
                sb_cmd.Append(sb.ToString).Append(cmd_str.First)

                Using cmd As System.Data.Common.DbCommand = DbSchema.CreateDBCommand
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

            values.Clear()
            Dim dic As IDictionary(Of Integer, T) = GetDictionary(Of T)()
            If remove_not_found Then
                For Each o As T In objs
                    If o.IsLoaded Then
                        values.Add(o)
                    ElseIf ListConverter.IsWeak Then
                        Dim obj As T = Nothing
                        If dic.TryGetValue(o.Identifier, obj) AndAlso obj.IsLoaded Then
                            values.Add(obj)
                        End If
                    End If
                Next
            Else
                If ListConverter.IsWeak Then
                    For Each o As T In objs
                        Dim obj As T = Nothing
                        If dic.TryGetValue(o.Identifier, obj) Then
                            values.Add(obj)
                        Else
                            values.Add(o)
                        End If
                    Next
                Else
                    Return objs
                End If
            End If
            Return values
        End Function

        Protected Function GetFilters(ByVal ids As Generic.List(Of Integer), ByVal fieldName As String, _
            ByVal almgr As AliasMgr, ByVal params As ParamMgr, ByVal original_type As Type, ByVal idsSorted As Boolean) As Generic.IEnumerable(Of Pair(Of String, Integer))

            Dim mr As MergeResult = MergeIds(ids, Not idsSorted)

            Dim l As New Generic.List(Of Pair(Of String, Integer))

            If mr IsNot Nothing Then
                Dim sb As New StringBuilder

                For Each p As Pair(Of Integer) In mr.Pairs
                    Dim con As New Orm.Condition.ConditionConstructor
                    con.AddFilter(New EntityFilter(original_type, fieldName, New ScalarValue(p.First), FilterOperation.GreaterEqualThan))
                    con.AddFilter(New EntityFilter(original_type, fieldName, New ScalarValue(p.Second), FilterOperation.LessEqualThan))
                    sb.Append(con.Condition.MakeSQLStmt(DbSchema, almgr, params))
                    If sb.Length > DbSchema.QueryLength Then
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

                        If sb2.Length > DbSchema.QueryLength - sb.Length Then
                            sb2.Length -= 1
                            sb2.Append(")")
                            Dim f As New EntityFilter(original_type, fieldName, New LiteralValue(sb2.ToString), FilterOperation.In)

                            sb.Append(f.MakeSQLStmt(DbSchema, almgr, params))

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
                        Dim f As New EntityFilter(original_type, fieldName, New LiteralValue(sb2.ToString), FilterOperation.In)
                        sb.Append(f.MakeSQLStmt(DbSchema, almgr, params))

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

        Protected Function GetFilters(ByVal ids As Generic.List(Of Integer), ByVal table As OrmTable, ByVal column As String, _
            ByVal almgr As AliasMgr, ByVal params As ParamMgr, ByVal idsSorted As Boolean) As Generic.IEnumerable(Of Pair(Of String, Integer))

            Dim mr As MergeResult = MergeIds(ids, Not idsSorted)

            Dim l As New Generic.List(Of Pair(Of String, Integer))

            If mr IsNot Nothing Then
                Dim sb As New StringBuilder

                For Each p As Pair(Of Integer) In mr.Pairs
                    Dim con As New Orm.Condition.ConditionConstructor
                    con.AddFilter(New TableFilter(table, column, New ScalarValue(p.First), FilterOperation.GreaterEqualThan))
                    con.AddFilter(New TableFilter(table, column, New ScalarValue(p.Second), FilterOperation.LessEqualThan))
                    sb.Append(con.Condition.MakeSQLStmt(DbSchema, almgr, params))
                    If sb.Length > DbSchema.QueryLength Then
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

                        If sb2.Length > DbSchema.QueryLength - sb.Length Then
                            sb2.Length -= 1
                            sb2.Append(")")
                            Dim f As New TableFilter(table, column, New LiteralValue(sb2.ToString), FilterOperation.In)

                            sb.Append(f.MakeSQLStmt(DbSchema, almgr, params))

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
                        Dim f As New TableFilter(table, column, New LiteralValue(sb2.ToString), FilterOperation.In)
                        sb.Append(f.MakeSQLStmt(DbSchema, almgr, params))

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

        Private Class FProxy

            Private _t As Type

            Public Sub New(ByVal t As Type)
                _t = t
            End Sub

            Public Function GetValue(ByVal tokens() As String, ByVal sectionName As String, ByVal f As IOrmFullTextSupport) As String
                Return Configuration.SearchSection.GetValueForFreeText(_t, tokens, sectionName)
            End Function
        End Class

        Protected Overrides Function Search(Of T As {New, OrmBase})(ByVal type2search As Type, ByVal tokens() As String, _
            ByVal contextKey As Object, ByVal sort As Sort, ByVal filter As IFilter) As System.Collections.Generic.ICollection(Of T)

            Dim fields As New List(Of Pair(Of String, Type))
            Dim searchSchema As IOrmObjectSchema = DbSchema.GetObjectSchema(type2search)
            Dim selectType As System.Type = GetType(T)
            Dim selSchema As IOrmObjectSchema = DbSchema.GetObjectSchema(selectType)
            Dim fsearch As IOrmFullTextSupport = TryCast(searchSchema, IOrmFullTextSupport)
            Dim queryFields As String() = Nothing
            Dim selCols, searchCols As New List(Of ColumnAttribute)
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
            If selectType IsNot type2search Then
                selCols.Insert(0, New ColumnAttribute("ID"))
                fields.Insert(0, New Pair(Of String, Type)("ID", selectType))
            End If

            Dim types As New List(Of Type)
            Dim joins As New List(Of OrmJoin)

            If selectType IsNot type2search Then
                Dim field As String = _schema.GetJoinFieldNameByType(selectType, type2search, selSchema)

                If String.IsNullOrEmpty(field) Then
                    field = _schema.GetJoinFieldNameByType(type2search, selectType, searchSchema)

                    If String.IsNullOrEmpty(field) Then
                        Throw New OrmManagerException(String.Format("Relation {0} to {1} is ambiguous or not exist. Use FindJoin method", selectType, type2search))
                    End If

                    joins.Add(MakeJoin(selectType, type2search, field, FilterOperation.Equal, JoinType.Join))
                Else
                    joins.Add(MakeJoin(type2search, selectType, field, FilterOperation.Equal, JoinType.Join, True))
                End If

                types.Add(selectType)
            End If

            Dim appendMain As Boolean = False
            If sort IsNot Nothing Then
                Dim ns As Sort = sort
                Do
                    Dim sortType As System.Type = ns.Type
                    ns = ns.Previous
                    If sortType Is Nothing Then
                        sortType = selectType
                    End If
                    appendMain = type2search Is sortType OrElse appendMain
                    If Not types.Contains(sortType) Then
                        If type2search IsNot sortType Then
                            Dim srtschema As IOrmObjectSchemaBase = _schema.GetObjectSchema(sortType)
                            Dim field As String = _schema.GetJoinFieldNameByType(type2search, sortType, searchSchema)
                            If Not String.IsNullOrEmpty(field) Then
                                joins.Add(MakeJoin(sortType, type2search, field, FilterOperation.Equal, JoinType.Join))
                                types.Add(sortType)
                                Continue Do
                            End If

                            'field = _schema.GetJoinFieldNameByType(sortType, type2search, srtschema)
                            'If Not String.IsNullOrEmpty(field) Then
                            '    joins.Add(MakeJoin(type2search, sortType, field, FilterOperation.Equal, JoinType.Join, True))
                            '    Continue Do
                            'End If

                            If selectType IsNot type2search Then
                                'field = _schema.GetJoinFieldNameByType(sortType, selectType, srtschema)
                                'If Not String.IsNullOrEmpty(field) Then
                                '    joins.Add(MakeJoin(sortType, selectType, field, FilterOperation.Equal, JoinType.Join))
                                '    Continue Do
                                'End If

                                field = _schema.GetJoinFieldNameByType(selectType, sortType, selSchema)
                                If Not String.IsNullOrEmpty(field) Then
                                    joins.Add(MakeJoin(selectType, sortType, field, FilterOperation.Equal, JoinType.Join, True))
                                    types.Add(sortType)
                                    Continue Do
                                End If
                            End If

                            If String.IsNullOrEmpty(field) Then
                                Throw New OrmManagerException(String.Format("Relation {0} to {1} is ambiguous or not exist. Use FindJoin method", selectType, type2search))
                            End If
                        End If
                    End If
                Loop While ns IsNot Nothing
            End If

            If filter IsNot Nothing Then
                For Each f As IFilter In filter.GetAllFilters
                    Dim ef As IEntityFilter = TryCast(f, IEntityFilter)
                    If ef IsNot Nothing Then
                        Dim type2join As System.Type = CType(ef.GetFilterTemplate, OrmFilterTemplate).Type
                        appendMain = type2search Is type2join OrElse appendMain
                        If type2search IsNot type2join Then
                            If Not types.Contains(type2join) Then
                                Dim field As String = _schema.GetJoinFieldNameByType(type2search, type2join, searchSchema)

                                If Not String.IsNullOrEmpty(field) Then
                                    joins.Add(MakeJoin(type2join, type2search, field, FilterOperation.Equal, JoinType.Join))
                                    types.Add(type2join)
                                Else
                                    If selectType IsNot type2search Then
                                        field = _schema.GetJoinFieldNameByType(selectType, type2join, selSchema)
                                        If Not String.IsNullOrEmpty(field) Then
                                            joins.Add(MakeJoin(selectType, type2join, field, FilterOperation.Equal, JoinType.Join, True))
                                            types.Add(type2join)
                                            Continue For
                                        End If
                                    End If

                                    Throw New OrmManagerException(String.Format("Relation {0} to {1} is ambiguous or not exist. Use FindJoin method", selectType, type2join))
                                End If
                            End If
                        End If
                    Else
                        Dim tf As TableFilter = TryCast(f, TableFilter)
                        If tf IsNot Nothing Then
                            If tf.Template.Table IsNot selSchema.GetTables(0) Then
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
                    searchCols.Insert(0, New ColumnAttribute("ID"))
                    fields.Insert(0, New Pair(Of String, Type)("ID", type2search))
                End If

                If contextKey IsNot Nothing Then
                    queryFields = fsearch.GetQueryFields(contextKey)
                End If
            End If

            Dim col As ICollection(Of T) = Nothing
            Using cmd As System.Data.Common.DbCommand = DbSchema.CreateDBCommand
                'Dim cols As Generic.List(Of ColumnAttribute) = Nothing

                'If fields.Count > 0 Then
                '    cols = New List(Of ColumnAttribute)
                '    cols.Add(DbSchema.GetColumnByFieldName(selectType, "ID"))
                '    For Each f As Pair(Of String, Type) In fields
                '        cols.Add(DbSchema.GetColumnByFieldName(f.Second, f.First))
                '    Next
                'End If

                With cmd
                    .CommandType = System.Data.CommandType.Text

                    Dim params As New ParamMgr(DbSchema, "p")
                    .CommandText = DbSchema.MakeSearchStatement(type2search, selectType, tokens, fields, _
                        GetSearchSection, joins, SortType.Desc, params, GetFilterInfo, queryFields, _
                        Integer.MinValue, AddressOf Configuration.SearchSection.GetValueForContains, _
                        "containstable", sort, appendMain, filter)
                    params.AppendParams(.Parameters)
                End With

                If type2search Is selectType OrElse searchCols.Count = 0 Then
                    col = CType(LoadMultipleObjects(Of T)(cmd, fields IsNot Nothing, Nothing, selCols), List(Of T))
                Else
                    col = CType(LoadMultipleObjects(selectType, type2search, cmd, selCols, searchCols), List(Of T))
                End If
            End Using

            Dim col2 As ICollection(Of T) = Nothing
            If tokens.Length > 1 Then
                Using cmd As System.Data.Common.DbCommand = DbSchema.CreateDBCommand
                    With cmd
                        .CommandType = System.Data.CommandType.Text

                        Dim params As New ParamMgr(DbSchema, "p")
                        .CommandText = DbSchema.MakeSearchStatement(type2search, selectType, tokens, fields, _
                            GetSearchSection, joins, SortType.Desc, params, GetFilterInfo, queryFields, 500, _
                            AddressOf New FProxy(type2search).GetValue, "freetexttable", sort, appendMain, filter)
                        params.AppendParams(.Parameters)
                    End With

                    If type2search Is selectType OrElse searchCols.Count = 0 Then
                        col2 = CType(LoadMultipleObjects(Of T)(cmd, fields IsNot Nothing, Nothing, selCols), List(Of T))
                    Else
                        col2 = CType(LoadMultipleObjects(selectType, type2search, cmd, selCols, searchCols), List(Of T))
                    End If
                End Using
            End If

            Dim res As List(Of T) = Nothing
            If fields IsNot Nothing AndAlso selectType Is type2search AndAlso sort Is Nothing Then
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
                        Dim s As String = CStr(o.GetValue(f, searchSchema))
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
                Dim dic As New Dictionary(Of Integer, T)
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

            Return res
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

        Protected Overrides Function BuildDictionary(Of T As {New, OrmBase})(ByVal level As Integer, ByVal filter As IFilter, ByVal join As OrmJoin) As DicIndex(Of T)
            Invariant()
            Dim params As New Orm.ParamMgr(DbSchema, "p")
            Using cmd As System.Data.Common.DbCommand = DbSchema.CreateDBCommand
                cmd.CommandText = DbSchema.GetDictionarySelect(GetType(T), level, params, filter, GetFilterInfo)
                cmd.CommandType = System.Data.CommandType.Text
                params.AppendParams(cmd.Parameters)
                Dim b As ConnAction = TestConn(cmd)
                Try
                    Dim root As DicIndex(Of T) = BuildDictionaryInternal(Of T)(cmd, level, Me)
                    root.Filter = filter
                    root.Join = join
                    Return root
                Finally
                    CloseConn(b)
                End Try
            End Using
        End Function

        Protected Shared Function BuildDictionaryInternal(Of T As {New, OrmBase})(ByVal cmd As System.Data.Common.DbCommand, ByVal level As Integer, ByVal mgr As OrmReadOnlyDBManager) As DicIndex(Of T)
            Dim last As DicIndex(Of T) = New DicIndex(Of T)("ROOT", Nothing, 0)
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

                    BuildDic(Of T)(name, cnt, level, root, last, first)
                Loop
                If mgr IsNot Nothing Then
                    mgr._fetch = ft.GetTime
                End If
            End Using

            'Dim tt As Type = GetType(MediaIndex(Of T))
            'Return CType(arr1.ToArray(tt), MediaIndex(Of T)())
            Return root
        End Function

        Protected Friend Overrides Sub SaveObject(ByVal obj As OrmBase)
            Throw New NotImplementedException()
        End Sub

        Public Overrides Function Add(ByVal obj As OrmBase) As OrmBase
            Throw New NotImplementedException()
        End Function

        Protected Friend Overrides Sub DeleteObject(ByVal obj As OrmBase)
            Throw New NotImplementedException()
        End Sub

        'Protected Overrides Sub Obj2ObjRelationSave2(ByVal obj As OrmBase, ByVal dt As System.Data.DataTable, ByVal sync As String, ByVal t As System.Type)
        '    Throw New NotImplementedException()
        'End Sub

        Protected Overrides Sub M2MSave(ByVal obj As OrmBase, ByVal t As System.Type, ByVal direct As Boolean, ByVal el As EditableList)
            Throw New NotImplementedException
        End Sub

        Public Overrides Function SaveAll(ByVal obj As OrmBase, ByVal AcceptChanges As Boolean) As Boolean
            Throw New NotImplementedException()
        End Function

        Protected Overrides Function GetSearchSection() As String
            Return String.Empty
        End Function

        Protected Overrides Function MakeJoin(ByVal type2join As Type, ByVal selectType As Type, ByVal field As String, _
            ByVal oper As FilterOperation, ByVal joinType As JoinType, Optional ByVal switchTable As Boolean = False) As OrmJoin

            Dim tbl As OrmTable = DbSchema.GetTables(type2join)(0)
            If switchTable Then
                tbl = DbSchema.GetTables(selectType)(0)
            End If

            Dim jf As New JoinFilter(type2join, "ID", selectType, field, oper)

            Return New OrmJoin(tbl, joinType, jf)
        End Function

        Protected Overrides ReadOnly Property Exec() As System.TimeSpan
            Get
                Return _exec
            End Get
        End Property

        Protected Overrides ReadOnly Property Fecth() As System.TimeSpan
            Get
                Return _fetch
            End Get
        End Property
    End Class
End Namespace
