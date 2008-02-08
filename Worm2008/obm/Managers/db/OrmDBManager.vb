Imports Worm
Imports System.Collections.Generic
Imports Worm.Cache
Imports Worm.Orm
Imports Worm.Database.Criteria.Core
Imports Worm.Sorting
Imports Worm.Orm.Meta

Namespace Database
    Public Class OrmDBManager
        Inherits OrmReadOnlyDBManager

        Public Sub New(ByVal cache As OrmCacheBase, ByVal schema As DbSchema, ByVal connectionString As String)
            MyBase.New(cache, schema, connectionString)
        End Sub

        Protected Sub New(ByVal schema As DbSchema, ByVal connectionString As String)
            MyBase.New(schema, connectionString)
        End Sub

        Protected Friend Overrides Sub SaveObject(ByVal obj As OrmBase)
            Invariant()

            If obj Is Nothing Then
                Throw New ArgumentNullException("obj parameter cannot be nothing")
            End If

            Assert(obj.ObjectState = ObjectState.Modified, "Object " & obj.Identifier & " should be in Modified state")
            'Dim t As Type = obj.GetType

            Dim params As IEnumerable(Of System.Data.Common.DbParameter) = Nothing
            Dim cols As Generic.IList(Of ColumnAttribute) = Nothing
            Dim upd As IList(Of EntityFilter) = Nothing
            Dim inv As Boolean
            Using obj.GetSyncRoot()
                Dim cmdtext As String = DbSchema.Update(obj, GetFilterInfo, params, cols, upd)
                If cmdtext.Length > 0 Then
                    If DbSchema.SupportMultiline Then
                        Using cmd As System.Data.Common.DbCommand = DbSchema.CreateDBCommand
                            Dim b As ConnAction = TestConn(cmd)
                            Try
                                With cmd
                                    .CommandType = System.Data.CommandType.Text
                                    .CommandText = cmdtext
                                    For Each p As System.Data.Common.DbParameter In params
                                        .Parameters.Add(p)
                                    Next
                                End With

                                LoadSingleObject(cmd, cols, obj, False, False, False)

                                inv = True
                            Finally
                                CloseConn(b)
                            End Try

                        End Using
                    Else
                        Dim tran As System.Data.Common.DbTransaction = Transaction
                        BeginTransaction()
                        Try
                            Dim prev_error As Boolean = False
                            For Each stmt As String In Microsoft.VisualBasic.Split(cmdtext, DbSchema.EndLine)
                                If stmt = String.Empty Then Continue For
                                Using cmd As System.Data.Common.DbCommand = DbSchema.CreateDBCommand
                                    Dim b As ConnAction = TestConn(cmd)
                                    Try
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
                                            Throw DbSchema.PrepareConcurrencyException(obj)
                                        End If

                                        prev_error = False

                                        If sel Then
                                            LoadSingleObject(cmd, cols, obj, False, False, False)
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
                                        CloseConn(b)
                                    End Try
                                End Using
                            Next

                            inv = True
                        Finally
                            If tran Is Nothing Then
                                Commit()
                            End If
                        End Try
                    End If
                End If
            End Using

            If inv Then
                InvalidateCache(obj, CType(upd, System.Collections.ICollection))
            End If
        End Sub

        Protected Overridable Sub InsertObject(ByVal obj As OrmBase)
            Invariant()

            If obj Is Nothing Then
                Throw New ArgumentNullException("obj parameter cannot be nothing")
            End If

            Assert(obj.ObjectState = ObjectState.Created, "Object " & obj.Identifier & " should be in Created state")

            Dim oldl As Boolean = obj.IsLoaded
            Dim err As Boolean = True
            Try
                obj.IsLoaded = True

                'Dim t As Type = obj.GetType

                Dim params As ICollection(Of System.Data.Common.DbParameter) = Nothing
                Dim cols As Generic.IList(Of ColumnAttribute) = Nothing
                Using obj.GetSyncRoot()
                    Dim cmdtext As String = DbSchema.Insert(obj, GetFilterInfo, params, cols)
                    If cmdtext.Length > 0 Then
                        Dim tran As System.Data.IDbTransaction = Transaction
                        BeginTransaction()
                        Try
                            If DbSchema.SupportMultiline Then
                                Using cmd As System.Data.Common.DbCommand = DbSchema.CreateDBCommand
                                    Dim b As ConnAction = TestConn(cmd)
                                    Try
                                        With cmd
                                            .CommandType = System.Data.CommandType.Text
                                            .CommandText = cmdtext
                                            For Each p As System.Data.IDataParameter In params
                                                .Parameters.Add(p)
                                            Next
                                        End With

                                        LoadSingleObject(cmd, cols, obj, False, False, True)
                                    Finally
                                        CloseConn(b)
                                    End Try
                                    'obj.AcceptChanges(cash)
                                End Using
                            Else
                                For Each stmt As String In Microsoft.VisualBasic.Split(cmdtext, DbSchema.EndLine)
                                    If stmt = "" Then Continue For
                                    Using cmd As System.Data.Common.DbCommand = DbSchema.CreateDBCommand
                                        Dim b As ConnAction = TestConn(cmd)
                                        Try
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

                                            If sel Then
                                                LoadSingleObject(cmd, cols, obj, False, False, True)
                                            Else
                                                cmd.ExecuteNonQuery()
                                            End If
                                        Finally
                                            CloseConn(b)
                                        End Try
                                    End Using
                                Next
                                'obj.AcceptChanges(cash)
                            End If
                        Finally
                            If tran Is Nothing Then
                                Commit()
                            End If
                        End Try
                    End If
                End Using
                err = False
            Finally
                If err Then obj.IsLoaded = oldl
            End Try
        End Sub

        Protected Overrides Sub M2MSave(ByVal obj As OrmBase, ByVal t As Type, ByVal direct As Boolean, ByVal el As EditableList)
            If obj Is Nothing Then
                Throw New ArgumentNullException("obj")
            End If

            If el Is Nothing Then
                Throw New ArgumentNullException("el")
            End If

            Dim tt As Type = obj.GetType
            Dim p As New ParamMgr(DbSchema, "p")
            Dim cmd_text As String = DbSchema.SaveM2M(obj, DbSchema.GetM2MRelation(tt, t, Not direct), el, p)

            If Not String.IsNullOrEmpty(cmd_text) Then
                Dim [error] As Boolean = True
                Dim tran As System.Data.Common.DbTransaction = Transaction
                BeginTransaction()
                Try
                    Using cmd As New System.Data.SqlClient.SqlCommand(cmd_text)

                        Dim r As ConnAction = TestConn(cmd)
                        Try
                            With cmd
                                .CommandType = System.Data.CommandType.Text
                                p.AppendParams(.Parameters)
                            End With

                            Dim i As Integer = cmd.ExecuteNonQuery()
                            [error] = i = 0
                        Finally
                            CloseConn(r)
                        End Try
                    End Using
                Finally
                    If tran Is Nothing Then
                        If [error] Then
                            Rollback()
                        Else
                            Commit()
                        End If
                    End If
                End Try
            End If
        End Sub

        'Protected Overrides Sub Obj2ObjRelationSave2(ByVal obj As OrmBase, ByVal dt As System.Data.DataTable, ByVal sync As String, ByVal t As System.Type)
        'Throw New NotSupportedException
        'If obj Is Nothing Then
        '    Throw New ArgumentNullException("obj")
        'End If

        'If dt Is Nothing Then
        '    Throw New ArgumentNullException("dt")
        'End If

        'If sync Is Nothing Then
        '    Throw New ArgumentNullException("sync")
        'End If

        'If t Is Nothing Then
        '    Throw New ArgumentNullException("t")
        'End If

        'Dim mt As Type = obj.GetType

        'If Schema.IsMany2ManyReadonly(mt, t) Then
        '    Throw New InvalidOperationException("Relation is readonly")
        'End If

        'Using SyncHelper.AcquireDynamicLock(sync)
        '    Dim params As IList(Of System.Data.Common.DbParameter) = Nothing
        '    Dim selcmd As String = Schema.SelectM2M(obj, t, Nothing, Nothing, False, params)

        '    Dim da As System.Data.Common.DbDataAdapter = Schema.CreateDataAdapter
        '    da.SelectCommand = Schema.CreateDBCommand
        '    With da.SelectCommand
        '        .CommandText = selcmd
        '        .CommandType = System.Data.CommandType.Text
        '    End With
        '    da.TableMappings.Add(CType(Schema.GetJoinSelectMapping(mt, t), ICloneable).Clone)

        '    Dim r As ConnAction = TestConn(da.SelectCommand)
        '    Using cb As System.Data.Common.DbCommandBuilder = Schema.CreateCommandBuilder(da)
        '        Try
        '            With da.SelectCommand
        '                .Parameters.Add(Schema.CreateDBParameter(Schema.ParamName("p", 1), obj.Identifier))
        '            End With
        '            If Schema.NullIsZero Then
        '                Dim _da As System.Data.Odbc.OdbcDataAdapter = CType(da, System.Data.Odbc.OdbcDataAdapter)
        '                AddHandler _da.RowUpdating, AddressOf OdbcRowUpdatingEventHandler
        '            End If
        '            da.AcceptChangesDuringUpdate = False
        '            da.Update(dt)
        '        Finally
        '            With da.SelectCommand
        '                .Connection = Nothing
        '                .Transaction = Nothing
        '                .Parameters.Clear()
        '            End With
        '            CloseConn(r)
        '        End Try
        '    End Using
        'End Using
        'End Sub

        Protected Friend Overrides Sub DeleteObject(ByVal obj As OrmBase)
            Invariant()

            If obj Is Nothing Then
                Throw New ArgumentNullException("obj parameter cannot be nothing")
            End If

            Assert(obj.ObjectState = ObjectState.Deleted, "Object " & obj.Identifier & " should be in Deleted state")

            'Dim t As Type = obj.GetType

            Dim params As IEnumerable(Of System.Data.Common.DbParameter) = Nothing
            Using obj.GetSyncRoot()
                Dim cmdtext As String = DbSchema.Delete(obj, params, GetFilterInfo)
                If cmdtext.Length > 0 Then
                    Dim [error] As Boolean = True
                    Dim tran As System.Data.Common.DbTransaction = Transaction
                    BeginTransaction()
                    Try
                        If DbSchema.SupportMultiline Then
                            Using cmd As System.Data.Common.DbCommand = DbSchema.CreateDBCommand
                                Dim b As ConnAction = TestConn(cmd)
                                Try
                                    With cmd
                                        .CommandType = System.Data.CommandType.Text
                                        .CommandText = cmdtext
                                        For Each p As System.Data.Common.DbParameter In params
                                            .Parameters.Add(p)
                                        Next
                                    End With

                                    Dim i As Integer = cmd.ExecuteNonQuery

                                    [error] = i = 0
                                Finally
                                    CloseConn(b)
                                End Try
                            End Using
                        Else
                            For Each stmt As String In Microsoft.VisualBasic.Split(cmdtext, DbSchema.EndLine)
                                If stmt = "" Then Continue For
                                Using cmd As System.Data.Common.DbCommand = DbSchema.CreateDBCommand
                                    Dim b As ConnAction = TestConn(cmd)
                                    Try
                                        With cmd
                                            .CommandType = System.Data.CommandType.Text
                                            .CommandText = stmt
                                            Dim p As IList(Of System.Data.Common.DbParameter) = Nothing
                                            For j As Integer = 0 To ExtractParamsCount(stmt) - 1
                                                .Parameters.Add(CType(p(0), System.Data.IDataParameter))
                                                p.RemoveAt(0)
                                            Next
                                        End With

                                        Dim i As Integer = cmd.ExecuteNonQuery()
                                        If Not stmt.StartsWith("set") Then [error] = i = 0
                                    Finally
                                        CloseConn(b)
                                    End Try
                                End Using
                            Next
                        End If
                    Finally
                        If tran Is Nothing Then
                            If [error] Then
                                Rollback()
                            Else
                                Commit()
                            End If
                        End If
                    End Try

                    If [error] Then
                        Throw DbSchema.PrepareConcurrencyException(obj)
                    End If
                End If
            End Using
        End Sub

        Public Overrides Function Add(ByVal obj As OrmBase) As OrmBase
            Invariant()

            If obj Is Nothing Then
                Throw New ArgumentNullException("obj")
            End If

            Using obj.GetSyncRoot()
                If obj.ObjectState = ObjectState.Created OrElse obj.ObjectState = ObjectState.NotFoundInDB Then
                    InsertObject(obj)
                ElseIf obj.ObjectState = ObjectState.Clone Then
                    Throw New InvalidOperationException("Object with state " & obj.ObjectState.ToString & " cann't be added to cashe")
                End If
            End Using

            Return obj
        End Function

        Public Overrides Function SaveAll(ByVal obj As OrmBase, ByVal AcceptChanges As Boolean) As Boolean
            Dim old_id As Integer = 0
            Dim sa As SaveAction
            Dim state As ObjectState = obj.ObjectState
            If state = ObjectState.Created Then
                old_id = obj.Identifier
                sa = SaveAction.Insert
            End If

            If state = ObjectState.Deleted Then
                sa = SaveAction.Delete
            End If
            Dim t As Type = obj.GetType
            Dim old_state As ObjectState = state
            Dim hasNew As Boolean = False
            Try
#If DebugLocks Then
                Using SyncHelper.AcquireDynamicLock_Debug("4098jwefpv345mfds-" & t.ToString & obj.Identifier, "d:\temp\")
#Else
                Using SyncHelper.AcquireDynamicLock("4098jwefpv345mfds-" & t.ToString & obj.Identifier)
#End If
                    Dim processedType As New List(Of Type)
                    If sa = SaveAction.Delete Then
                        For Each r As M2MRelation In DbSchema.GetM2MRelations(t)
                            Dim acs As OrmBase.AcceptState2 = Nothing
                            If r.ConnectedType Is Nothing Then
                                If r.DeleteCascade Then
                                    M2MDelete(obj, r.Type, Not r.non_direct)
                                End If
                                acs = M2MSave(obj, r.Type, Not r.non_direct)
                                processedType.Add(r.Type)
                            End If
                            'Obj2ObjRelationReset(obj, r.Type)
                            'Dim acs As New OrmBase.AcceptState(Nothing, Nothing, Nothing, r.Type)
                            'If r.ConnectedType Is Nothing Then
                            '    Dim dt As System.Data.DataTable = Nothing
                            '    If r.DeleteCascade Then
                            '        dt = Obj2ObjRelationDeleteInternal(obj, r.Type)
                            '    End If
                            '    If dt Is Nothing Then
                            '        acs = Obj2ObjRelationSave2(obj, r.Type)
                            '        processedType.Add(r.Type)
                            '    Else
                            '        Dim tt1 As Type = obj.GetType
                            '        Dim tt2 As Type = r.Type

                            '        Dim key As String = tt1.Name & Const_JoinStaticString & tt2.Name & GetStaticKey()
                            '        Dim id As String = obj.Identifier.ToString

                            '        Obj2ObjRelationSave2(obj, dt, id & key & "901hfn013nvc0nvvl", r.Type)
                            '        acs = New OrmBase.AcceptState(dt, id, key, r.Type)
                            '        processedType.Add(r.Type)
                            '        'ResetAllM2MRelations(id, key)
                            '    End If
                            'End If
                            ''Obj2ObjRelationRemove(obj, r.Type)
                            If acs IsNot Nothing Then obj.AddAccept(acs)
                        Next

                        Dim oo As IRelation = TryCast(DbSchema.GetObjectSchema(t), IRelation)
                        If oo IsNot Nothing Then
                            Dim o As New M2MEnum(oo, obj, DbSchema)
                            Cache.ConnectedEntityEnum(t, AddressOf o.Remove)
                        End If
                    End If

                    obj.Save(Me)
                    obj.RaiseSaved(sa)

                    If sa = SaveAction.Insert Then
                        Dim oo As IRelation = TryCast(DbSchema.GetObjectSchema(t), IRelation)
                        If oo IsNot Nothing Then
                            Dim o As New M2MEnum(oo, obj, DbSchema)
                            Cache.ConnectedEntityEnum(t, AddressOf o.Add)
                        End If

                        M2MUpdate(obj, old_id)

                        For Each r As M2MRelation In DbSchema.GetM2MRelations(t)
                            Dim tt As Type = r.Type
                            If Not DbSchema.IsMany2ManyReadonly(t, tt) Then
                                Dim acs As OrmBase.AcceptState2 = M2MSave(obj, tt, Not r.non_direct)
                                If acs IsNot Nothing Then
                                    hasNew = hasNew OrElse acs.el.HasNew
                                    'obj.AddAccept(acs)
                                End If
                            End If
                        Next
                    ElseIf sa = SaveAction.Update Then
                        If obj._needAccept IsNot Nothing Then
                            For Each acp As OrmBase.AcceptState2 In obj._needAccept
                                'Dim el As EditableList = acp.el.PrepareNewSave(Me)
                                Dim el As EditableList = acp.el.PrepareSave(Me)
                                If el IsNot Nothing Then
                                    M2MSave(obj, acp.el.SubType, acp.el.Direct, el)
                                    acp.CacheItem.Entry.Saved = True
                                End If
                                hasNew = hasNew OrElse acp.el.HasNew
                                processedType.Add(acp.el.SubType)
                            Next
                        End If
                        For Each o As Pair(Of OrmManagerBase.M2MCache, Pair(Of String, String)) In Cache.GetM2MEntries(obj, Nothing)
                            'Dim m As M2MCache = o.First
                            'If Not Schema.IsMany2ManyReadonly(t, m.Entry.SubType) AndAlso Not processedType.Contains(m.Entry.SubType) Then
                            '    'Dim r As M2MRelation = Schema.GetM2MRelation(t, m.Entry.SubType, m.Entry.Direct)
                            '    Dim acs As OrmBase.AcceptState2 = M2MSave(obj, m.Entry.SubType, m.Entry.Direct)
                            '    If acs IsNot Nothing Then
                            '        Dim hasNew1 As Boolean = acs.added IsNot Nothing AndAlso acs.added.Count > 0
                            '        hasNew = hasNew Or hasNew1
                            '        obj.AddAccept(acs)
                            '    End If
                            'End If
                            Dim m2me As M2MCache = o.First
                            If m2me.Filter IsNot Nothing Then
                                Dim dic As IDictionary = GetDic(_cache, o.Second.First)
                                dic.Remove(o.Second.Second)
                            Else
                                If m2me.Entry.HasChanges AndAlso Not m2me.Entry.Saved AndAlso Not processedType.Contains(m2me.Entry.SubType) Then
                                    Throw New InvalidOperationException
                                End If

                                'If m2me.Entry.HasChanges AndAlso Not m2me.Entry.Saved AndAlso Not processedType.Contains(m2me.Entry.SubType) Then
                                '    Using SyncHelper.AcquireDynamicLock(GetSync(o.Second.First, o.Second.Second))
                                '        'Dim tt1 As Type = obj.GetType
                                '        Dim tt2 As Type = m2me.Entry.SubType
                                '        'Dim added As New List(Of Integer)
                                '        Dim sv As EditableList = m2me.Entry.PrepareSave(Me)
                                '        If sv IsNot Nothing Then
                                '            M2MSave(obj, tt2, m2me.Entry.Direct, sv)
                                '            m2me.Entry.Saved = True
                                '        End If
                                '        'Dim acs As New OrmBase.AcceptState2(m2me, o.Second.First, o.Second.Second)
                                '        'If acs IsNot Nothing Then
                                '        '    hasNew = hasNew Or acs.el.HasNew
                                '        '    obj.AddAccept(acs)
                                '        'End If
                                '        Dim acs As OrmBase.AcceptState2 = obj.GetAccept(m2me)
                                '        hasNew = hasNew Or acs.el.HasNew
                                '    End Using
                                'End If
                            End If
                        Next
                    End If

                    If AcceptChanges Then
                        If hasNew Then
                            Throw New OrmObjectException("Cannot accept changes. Some of relation has new objects")
                        End If
                        obj.AcceptChanges(True, OrmBase.IsGoogState(state))
                    End If

                End Using
            Catch
                If sa = SaveAction.Insert Then
                    obj.RejectChanges()
                End If

                state = old_state
                Throw
            End Try
            Return hasNew
        End Function

        'Public Overloads Sub DeleteRelation2(ByVal obj As OrmBase, ByVal t As Type)
        'Dim dt As System.Data.DataTable = Obj2ObjRelationDeleteInternal(obj, t)
        'Dim tt1 As Type = obj.GetType
        'Dim tt2 As Type = t

        'Dim key As String = tt1.Name & Const_JoinStaticString & tt2.Name & GetStaticKey()
        'Dim id As String = obj.Identifier.ToString

        'obj._needAccept.Add(New OrmBase.AcceptState(dt, id, key, t))
        'Obj2ObjRelationSave(obj, dt, id & key & "901hfn013nvc0nvvl", t)
        'ResetAllM2MRelations(id, key)
        'End Sub

        Public Function Delete(ByVal f As IEntityFilter) As Integer
            Dim t As Type = Nothing
#If DEBUG Then
            For Each fl As EntityFilter In f.GetAllFilters
                If t Is Nothing Then
                    t = fl.Template.Type
                ElseIf t IsNot fl.Template.Type Then
                    Throw New InvalidOperationException("All filters must have the same type")
                End If
            Next
#End If
            Using cmd As System.Data.Common.DbCommand = DbSchema.CreateDBCommand
                Dim r As ConnAction = TestConn(cmd)
                Try
                    Dim params As New ParamMgr(DbSchema, "p")
                    With cmd
                        .CommandText = DbSchema.Delete(t, f, params)
                        .CommandType = System.Data.CommandType.Text
                        params.AppendParams(.Parameters)
                        Return .ExecuteNonQuery()
                    End With
                Finally
                    CloseConn(r)
                End Try
            End Using
        End Function

        'Protected Sub OdbcRowUpdatingEventHandler(ByVal sender As Object, ByVal e As System.Data.Odbc.OdbcRowUpdatingEventArgs)
        '    If e.Command IsNot Nothing Then
        '        With e.Command
        '            For Each p As System.Data.Odbc.OdbcParameter In .Parameters
        '                Dim s As String = "(" & p.SourceColumn & " = ?"
        '                Dim pos As Integer = .CommandText.IndexOf(s)
        '                If pos > 0 AndAlso p.Value Is DBNull.Value Then
        '                    '.CommandText.Insert(pos, "(")
        '                    Dim pos2 As Integer = pos + s.Length
        '                    .CommandText = .CommandText.Insert(pos2, " or " & p.SourceColumn & " = 0")
        '                End If
        '            Next
        '        End With
        '    End If
        'End Sub

    End Class

End Namespace
