﻿Imports Worm.Database
Imports System.Collections.Generic
Imports Worm.Orm
Imports Worm.Orm.Meta
Imports Worm.Database.Criteria.Joins
Imports Worm.Criteria.Core

Namespace Query.Database

    <Serializable()> _
    Public Class ExecutorException
        Inherits System.Exception

        Public Sub New(ByVal message As String)
            MyBase.New(message)
        End Sub

        Public Sub New(ByVal message As String, ByVal inner As Exception)
            MyBase.New(message, inner)
        End Sub

        Private Sub New( _
            ByVal info As System.Runtime.Serialization.SerializationInfo, _
            ByVal context As System.Runtime.Serialization.StreamingContext)
            MyBase.New(info, context)
        End Sub
    End Class

    Partial Public Class DbQueryExecutor
        Implements IExecutor

        Private Const RowNumberOrder As String = "qiervfnkasdjvn"

        Private _proc As Object
        Private _procT As Object
        Private _m As Integer
        Private _sm As Integer

        Protected Function GetProcessorAnonym(Of ReturnType As {_IEntity})(ByVal mgr As OrmManagerBase, ByVal query As QueryCmdBase) As ProcessorEntity(Of ReturnType)
            If _proc Is Nothing Then
                Dim j As New List(Of List(Of Worm.Criteria.Joins.OrmJoin))
                'If query.Joins IsNot Nothing Then
                '    j.AddRange(query.Joins)
                'End If

                If query.SelectedType Is Nothing Then
                    If String.IsNullOrEmpty(query.EntityName) Then
                        query.SelectedType = GetType(ReturnType)
                    Else
                        query.SelectedType = mgr.ObjectSchema.GetTypeByEntityName(query.EntityName)
                    End If
                End If

                Dim f() As IFilter = query.Prepare(j, mgr.ObjectSchema, mgr.GetFilterInfo, query.SelectedType)
                'If query.Filter IsNot Nothing Then
                '    f = query.Filter.Filter(GetType(ReturnType))
                'End If

                'If query.AutoJoins Then
                '    Dim joins() As Worm.Criteria.Joins.OrmJoin = Nothing
                '    If mgr.HasJoins(GetType(ReturnType), f, query.Sort, joins) Then
                '        j.AddRange(joins)
                '    End If
                'End If

                'If query.Obj IsNot Nothing Then
                '    _proc = New M2MProcessor(Of ReturnType)(CType(mgr, OrmReadOnlyDBManager), j, f, query)
                'Else
                _proc = New ProcessorEntity(Of ReturnType)(CType(mgr, OrmReadOnlyDBManager), j, f, query)
                'End If

                _m = query.Mark
                _sm = query.SMark
            Else
                Dim p As ProcessorEntity(Of ReturnType) = CType(_proc, ProcessorEntity(Of ReturnType))
                If _m <> query.Mark Then
                    Dim j As New List(Of List(Of Worm.Criteria.Joins.OrmJoin))
                    Dim f() As IFilter = query.Prepare(j, mgr.ObjectSchema, mgr.GetFilterInfo, query.SelectedType)
                    p.Reset(j, f)
                ElseIf _sm <> query.SMark Then
                    p.ResetStmt()
                End If
            End If

            Return CType(_proc, ProcessorEntity(Of ReturnType))
        End Function

        Protected Function GetProcessor(Of ReturnType As {ICachedEntity})(ByVal mgr As OrmManagerBase, ByVal query As QueryCmdBase) As Processor(Of ReturnType)
            If _proc Is Nothing Then
                Dim j As New List(Of List(Of Worm.Criteria.Joins.OrmJoin))
                'If query.Joins IsNot Nothing Then
                '    j.AddRange(query.Joins)
                'End If

                If query.SelectedType Is Nothing Then
                    If String.IsNullOrEmpty(query.EntityName) Then
                        query.SelectedType = GetType(ReturnType)
                    Else
                        query.SelectedType = mgr.ObjectSchema.GetTypeByEntityName(query.EntityName)
                    End If
                End If

                Dim f() As IFilter = query.Prepare(j, mgr.ObjectSchema, mgr.GetFilterInfo, query.SelectedType)
                'If query.Filter IsNot Nothing Then
                '    f = query.Filter.Filter(GetType(ReturnType))
                'End If

                'If query.AutoJoins Then
                '    Dim joins() As Worm.Criteria.Joins.OrmJoin = Nothing
                '    If mgr.HasJoins(GetType(ReturnType), f, query.Sort, joins) Then
                '        j.AddRange(joins)
                '    End If
                'End If

                'If query.Obj IsNot Nothing Then
                '    _proc = New M2MProcessor(Of ReturnType)(CType(mgr, OrmReadOnlyDBManager), j, f, query)
                'Else
                _proc = New Processor(Of ReturnType)(CType(mgr, OrmReadOnlyDBManager), j, f, query)
                'End If

                _m = query.Mark
                _sm = query.SMark
            Else
                Dim p As Processor(Of ReturnType) = CType(_proc, Processor(Of ReturnType))
                If _m <> query.Mark Then
                    Dim j As New List(Of List(Of Worm.Criteria.Joins.OrmJoin))
                    Dim f() As IFilter = query.Prepare(j, mgr.ObjectSchema, mgr.GetFilterInfo, query.SelectedType)
                    p.Reset(j, f)
                ElseIf _sm <> query.SMark Then
                    p.ResetStmt()
                End If
                p.Created = False
            End If

            Return CType(_proc, Processor(Of ReturnType))
        End Function

        Protected Function GetProcessorT(Of SelectType As {ICachedEntity, New}, ReturnType As {ICachedEntity})(ByVal mgr As OrmManagerBase, ByVal query As QueryCmdBase) As ProcessorT(Of SelectType, ReturnType)
            If _proc Is Nothing Then
                Dim j As New List(Of List(Of Worm.Criteria.Joins.OrmJoin))
                'If query.Joins IsNot Nothing Then
                '    j.AddRange(query.Joins)
                'End If

                Dim f() As IFilter = query.Prepare(j, mgr.ObjectSchema, mgr.GetFilterInfo, GetType(ReturnType))
                'If query.Filter IsNot Nothing Then
                '    f = query.Filter.Filter(GetType(ReturnType))
                'End If

                'If query.AutoJoins Then
                '    Dim joins() As Worm.Criteria.Joins.OrmJoin = Nothing
                '    If mgr.HasJoins(GetType(ReturnType), f, query.Sort, joins) Then
                '        j.AddRange(joins)
                '    End If
                'End If

                'If query.Obj IsNot Nothing Then
                '    _proc = New M2MProcessor(Of ReturnType)(CType(mgr, OrmReadOnlyDBManager), j, f, query)
                'Else
                _proc = New ProcessorT(Of SelectType, ReturnType)(CType(mgr, OrmReadOnlyDBManager), j, f, query)
                'End If

                _m = query.Mark
                _sm = query.SMark
            Else
                Dim p As Processor(Of ReturnType) = CType(_proc, Processor(Of ReturnType))
                If _m <> query.Mark Then
                    Dim j As New List(Of List(Of Worm.Criteria.Joins.OrmJoin))
                    Dim f() As IFilter = query.Prepare(j, mgr.ObjectSchema, mgr.GetFilterInfo, GetType(ReturnType))
                    p.Reset(j, f)
                ElseIf _sm <> query.SMark Then
                    p.ResetStmt()
                End If
                p.Created = False
            End If

            Return CType(_proc, ProcessorT(Of SelectType, ReturnType))
        End Function

        Public Function ExecSimple(Of SelectType As {New, _ICachedEntity}, ReturnType)(ByVal mgr As OrmManagerBase, ByVal query As QueryCmdBase) As System.Collections.Generic.IList(Of ReturnType) Implements IExecutor.ExecSimple
            Dim p As Processor(Of SelectType) = GetProcessor(Of SelectType)(mgr, query)

            Return p.GetSimpleValues(Of ReturnType)()
        End Function

        Public Function Exec(Of ReturnType As {_ICachedEntity})(ByVal mgr As OrmManagerBase, _
            ByVal query As QueryCmdBase) As ReadOnlyEntityList(Of ReturnType) Implements IExecutor.Exec

            Dim dontcache As Boolean = query.DontCache

            Dim key As String = Nothing
            Dim dic As IDictionary = Nothing
            Dim id As String = Nothing
            Dim sync As String = Nothing

            Dim p As Processor(Of ReturnType) = GetProcessor(Of ReturnType)(mgr, query)

            If Not dontcache Then
                key = p.Key
                id = p.Id
                dic = p.Dic
                sync = p.Sync
            End If

            Dim oldCache As Boolean = mgr._dont_cache_lists
            Dim oldStart As Integer = mgr._start
            Dim oldLength As Integer = mgr._length

            mgr._dont_cache_lists = dontcache OrElse query.OuterQuery IsNot Nothing
            If query.ClientPaging IsNot Nothing Then
                mgr._start = query.ClientPaging.First
                mgr._length = query.ClientPaging.Second
            End If

            Dim ce As OrmManagerBase.CachedItem = mgr.GetFromCache(Of ReturnType)(dic, sync, id, query.propWithLoad, p)
            query.LastExecitionResult = mgr.GetLastExecitionResult

            mgr._dont_cache_lists = oldCache
            mgr._start = oldStart
            mgr._length = oldLength

            mgr.RaiseOnDataAvailable()

            Dim s As Cache.IListObjectConverter.ExtractListResult
            'Dim r As ReadOnlyList(Of ReturnType) = ce.GetObjectList(Of ReturnType)(mgr, query.WithLoad, p.Created, s)
            'Return r
            Return ce.GetObjectList(Of ReturnType)(mgr, query.propWithLoad, p.Created, s)
        End Function

        Public Function Exec(Of SelectType As {_ICachedEntity, New}, ReturnType As {_ICachedEntity})(ByVal mgr As OrmManagerBase, _
            ByVal query As QueryCmdBase) As ReadOnlyEntityList(Of ReturnType) Implements IExecutor.Exec

            Dim dontcache As Boolean = query.DontCache

            Dim key As String = Nothing
            Dim dic As IDictionary = Nothing
            Dim id As String = Nothing
            Dim sync As String = Nothing

            Dim p As ProcessorT(Of SelectType, ReturnType) = GetProcessorT(Of SelectType, ReturnType)(mgr, query)

            If Not dontcache Then
                key = p.Key
                id = p.Id
                dic = p.Dic
                sync = p.Sync
            End If

            Dim oldCache As Boolean = mgr._dont_cache_lists
            Dim oldStart As Integer = mgr._start
            Dim oldLength As Integer = mgr._length

            mgr._dont_cache_lists = dontcache OrElse query.OuterQuery IsNot Nothing
            If query.ClientPaging IsNot Nothing Then
                mgr._start = query.ClientPaging.First
                mgr._length = query.ClientPaging.Second
            End If

            Dim ce As OrmManagerBase.CachedItem = mgr.GetFromCache(Of ReturnType)(dic, sync, id, query.propWithLoad, p)
            query.LastExecitionResult = mgr.GetLastExecitionResult

            mgr._dont_cache_lists = oldCache
            mgr._start = oldStart
            mgr._length = oldLength

            mgr.RaiseOnDataAvailable()

            Dim s As Cache.IListObjectConverter.ExtractListResult
            'Dim r As ReadOnlyList(Of ReturnType) = ce.GetObjectList(Of ReturnType)(mgr, query.WithLoad, p.Created, s)
            'Return r
            Return ce.GetObjectList(Of ReturnType)(mgr, query.propWithLoad, p.Created, s)
        End Function

#Region " Shared helpers "

        Protected Shared Function GetFields(ByVal gen As QueryGenerator, ByVal type As Type, ByVal q As QueryCmdBase) As List(Of ColumnAttribute)
            Dim c As IList(Of OrmProperty) = q.SelectList
            Dim l As List(Of ColumnAttribute) = Nothing
            If c IsNot Nothing Then
                l = New List(Of ColumnAttribute)
                For Each p As OrmProperty In c
                    'If Not type.Equals(p.Type) Then
                    '    Throw New NotImplementedException
                    'End If
                    If p.Type Is Nothing Then
                        If String.IsNullOrEmpty(p.Field) Then
                            Throw New InvalidOperationException(String.Format("Column {0} must have a field", p.Column))
                        End If
                        Dim cl As ColumnAttribute = gen.GetColumnByFieldName(type, p.Field)
                        If cl Is Nothing Then
                            cl = New ColumnAttribute
                            cl.FieldName = p.Field
                        End If
                        cl.Column = p.Column
                        l.Add(cl)
                    Else
                        l.Add(gen.GetColumnByFieldName(type, p.Field))
                    End If
                Next
                'l.Sort()
            Else
                If q.propWithLoad Then
                    l = gen.GetSortedFieldList(type)
                Else
                    l = gen.GetPrimaryKeys(type)
                End If
            End If

            If q.Aggregates IsNot Nothing Then
                For Each p As AggregateBase In q.Aggregates
                    Dim cl As New ColumnAttribute
                    cl.FieldName = p.Alias
                    cl.Column = p.Alias
                    l.Add(cl)
                Next
            End If
            Return l
        End Function

        Protected Shared Sub FormSelectList(ByVal query As QueryCmdBase, ByVal queryType As Type, _
            ByVal sb As StringBuilder, ByVal s As SQLGenerator, ByVal os As IOrmObjectSchema, _
            ByVal almgr As AliasMgr, ByVal filterInfo As Object, ByVal params As ICreateParam, _
            ByVal columnAliases As List(Of String), ByVal innerColumns As List(Of String))

            Dim b As Boolean
            Dim cols As New StringBuilder
            If os Is Nothing Then
                If query.SelectList IsNot Nothing Then
                    For Each p As OrmProperty In query.SelectList
                        If Not String.IsNullOrEmpty(p.Table.Name) Then
                            cols.Append(s.GetTableName(p.Table)).Append(".")
                        End If
                        cols.Append(p.Column).Append(", ")
                        columnAliases.Add(p.Column)
                    Next
                    cols.Length -= 2
                    sb.Append(cols.ToString)
                    b = True
                ElseIf innerColumns IsNot Nothing Then
                    For Each c As String In innerColumns
                        cols.Append(c).Append(",")
                        columnAliases.Add(c)
                    Next
                    cols.Length -= 1
                    sb.Append(cols.ToString)
                    b = True
                End If
            Else
                If query.propWithLoad Then
                    If query.SelectList Is Nothing AndAlso query.Aggregates Is Nothing Then
                        cols.Append(s.GetSelectColumnList(queryType, Nothing, columnAliases))
                        sb.Append(cols.ToString)
                        b = True
                    ElseIf query.SelectList IsNot Nothing Then
                        cols.Append(s.GetSelectColumnList(queryType, GetFields(s, queryType, query), columnAliases))
                        sb.Append(cols.ToString)
                        b = True
                    End If
                ElseIf query.SelectList IsNot Nothing Then
                    For Each p As OrmProperty In query.SelectList
                        Dim map As MapField2Column = os.GetFieldColumnMap()(p.Field)
                        cols.Append(s.GetTableName(map._tableName)).Append(".")
                        cols.Append(map._columnName).Append(", ")
                        columnAliases.Add(map._columnName)
                    Next
                    cols.Length -= 2
                    sb.Append(cols.ToString)
                    b = True
                ElseIf query.Aggregates Is Nothing Then
                    s.GetPKList(queryType, os, cols, columnAliases)
                    sb.Append(cols.ToString)
                    b = True
                End If
            End If

            If query.Aggregates IsNot Nothing Then
                For Each a As AggregateBase In query.Aggregates
                    If b Then
                        sb.Append(",")
                    Else
                        b = True
                    End If
                    sb.Append(a.MakeStmt(s, innerColumns, params, almgr))
                    If columnAliases IsNot Nothing Then
                        columnAliases.Add(a.GetAlias)
                    End If
                Next
            End If

            If query.RowNumberFilter IsNot Nothing Then
                If Not s.SupportRowNumber Then
                    Throw New NotSupportedException("RowNumber statement is not supported by " & s.Name)
                End If
                sb.Append(",row_number() over (")
                If query.propSort IsNot Nothing AndAlso Not query.propSort.IsExternal Then
                    sb.Append(RowNumberOrder)
                    'FormOrderBy(query, t, almgr, sb, s, filterInfo, params)
                Else
                    sb.Append("order by ").Append(cols.ToString)
                End If
                sb.Append(") as ").Append(QueryCmdBase.RowNumerColumn)
            End If
        End Sub

        Protected Shared Sub FormTypeTables(ByVal filterInfo As Object, ByVal params As ICreateParam, ByVal almgr As AliasMgr, ByVal sb As StringBuilder, ByVal s As SQLGenerator, ByVal os As IOrmObjectSchema, ByVal tables() As SourceFragment)
            Dim tbl As SourceFragment = tables(0)
            Dim tbl_real As SourceFragment = tbl
            Dim [alias] As String = Nothing
            If Not almgr.Aliases.TryGetValue(tbl, [alias]) Then
                [alias] = almgr.AddTable(tbl_real, params)
            Else
                tbl_real = tbl.OnTableAdd(params)
                If tbl_real Is Nothing Then
                    tbl_real = tbl
                End If
            End If

            'selectcmd = selectcmd.Replace(tbl.TableName & ".", [alias] & ".")
            almgr.Replace(s, tbl, sb)

            sb.Append(s.GetTableName(tbl_real)).Append(" ").Append([alias])

            For j As Integer = 1 To tables.Length - 1
                Dim join As OrmJoin = CType(s.GetJoins(os, tbl, tables(j), filterInfo), OrmJoin)

                If Not OrmJoin.IsEmpty(join) Then
                    If Not almgr.Aliases.ContainsKey(tables(j)) Then
                        almgr.AddTable(tables(j), CType(Nothing, ParamMgr))
                    End If
                    sb.Append(join.MakeSQLStmt(s, filterInfo, almgr, params))
                End If
            Next
        End Sub

        Protected Shared Sub FormJoins(ByVal filterInfo As Object, ByVal query As QueryCmdBase, ByVal params As ICreateParam, _
            ByVal j As List(Of Worm.Criteria.Joins.OrmJoin), ByVal almgr As AliasMgr, ByVal sb As StringBuilder, ByVal s As SQLGenerator)
            For i As Integer = 0 To j.Count - 1
                Dim join As OrmJoin = CType(j(i), OrmJoin)

                If Not OrmJoin.IsEmpty(join) Then
                    'Dim tbl As SourceFragment = join.Table
                    'If tbl Is Nothing Then
                    '    If join.Type IsNot Nothing Then
                    '    Else
                    '    End If
                    'End If
                    'almgr.AddTable(tbl, CType(Nothing, ParamMgr))
                    sb.Append(join.MakeSQLStmt(s, filterInfo, almgr, params))
                End If
            Next
        End Sub

        Protected Shared Sub FormGroupBy(ByVal query As QueryCmdBase, ByVal almgr As AliasMgr, ByVal sb As StringBuilder, ByVal s As SQLGenerator, ByVal selectType As Type)
            If query.Group IsNot Nothing Then
                sb.Append(" group by ")
                For Each g As OrmProperty In query.Group
                    If g.Table IsNot Nothing Then
                        sb.Append(almgr.Aliases(g.Table)).Append(".").Append(g.Column)
                    Else
                        Dim t As Type = g.Type
                        If t Is Nothing Then
                            t = selectType
                        End If
                        Dim schema As IOrmObjectSchema = s.GetObjectSchema(t)
                        Dim cm As Collections.IndexedCollection(Of String, MapField2Column) = schema.GetFieldColumnMap()
                        Dim map As MapField2Column = Nothing
                        If cm.TryGetValue(g.Field, map) Then
                            sb.Append(almgr.Aliases(map._tableName)).Append(".").Append(map._columnName)
                        Else
                            Throw New ArgumentException(String.Format("Field {0} of type {1} is not defined", g.Field, g.Type))
                        End If
                    End If
                    sb.Append(",")
                Next
                sb.Length -= 1
            End If
        End Sub

        Protected Shared Sub FormOrderBy(ByVal query As QueryCmdBase, ByVal t As Type, _
            ByVal almgr As AliasMgr, ByVal sb As StringBuilder, ByVal s As SQLGenerator, ByVal filterInfo As Object, _
            ByVal params As ICreateParam, ByVal columnAliases As List(Of String))
            If query.propSort IsNot Nothing AndAlso Not query.propSort.IsExternal Then
                Dim adv As Sorting.SortAdv = TryCast(query.propSort, Sorting.SortAdv)
                If adv IsNot Nothing Then
                    adv.MakeStmt(s, almgr, columnAliases, sb, t, filterInfo, params)
                Else
                    s.AppendOrder(t, query.propSort, almgr, sb, True, query.SelectList, query.Table)
                End If
            End If
        End Sub

        Public Shared Function MakeQueryStatement(ByVal filterInfo As Object, ByVal schema As SQLGenerator, _
            ByVal query As QueryCmdBase, ByVal params As ICreateParam, ByVal t As Type, _
            ByVal joins As List(Of Worm.Criteria.Joins.OrmJoin), ByVal f As IFilter, ByVal almgr As AliasMgr) As String

            Return MakeQueryStatement(filterInfo, schema, query, params, t, joins, f, almgr, Nothing, Nothing, Nothing, 0)
        End Function

        Public Shared Function MakeQueryStatement(ByVal filterInfo As Object, ByVal schema As SQLGenerator, _
            ByVal query As QueryCmdBase, ByVal params As ICreateParam, ByVal queryType As Type, _
            ByVal joins As List(Of Worm.Criteria.Joins.OrmJoin), ByVal f As IFilter, ByVal almgr As AliasMgr, _
            ByVal columnAliases As List(Of String), ByVal inner As String, ByVal innerColumns As List(Of String), ByVal i As Integer) As String

            Dim sb As New StringBuilder
            Dim s As SQLGenerator = schema
            Dim os As IOrmObjectSchema = Nothing

            If query.Table Is Nothing Then
                os = s.GetObjectSchema(queryType)
            End If

            sb.Append("select ")

            If query.propDistinct Then
                sb.Append("distinct ")
            End If

            If query.propTop IsNot Nothing Then
                sb.Append(s.TopStatement(query.propTop.Count, query.propTop.Percent, query.propTop.Ties)).Append(" ")
            End If

            FormSelectList(query, queryType, sb, s, os, almgr, filterInfo, params, columnAliases, innerColumns)

            sb.Append(" from ")

            If Not String.IsNullOrEmpty(inner) Then
                sb.Append("(").Append(inner).Append(") as src_t").Append(i)
            Else

                Dim tables() As SourceFragment = Nothing
                If os IsNot Nothing Then
                    tables = os.GetTables()
                Else
                    tables = New SourceFragment() {query.Table}
                End If

                FormTypeTables(filterInfo, params, almgr, sb, s, os, tables)

                FormJoins(filterInfo, query, params, joins, almgr, sb, s)
            End If

            s.AppendWhere(os, f, almgr, sb, filterInfo, params, innerColumns)

            FormGroupBy(query, almgr, sb, s, queryType)

            If query.RowNumberFilter Is Nothing Then
                FormOrderBy(query, queryType, almgr, sb, s, filterInfo, params, columnAliases)
            Else
                Dim r As New StringBuilder
                FormOrderBy(query, queryType, almgr, r, s, filterInfo, params, columnAliases)
                sb.Replace(RowNumberOrder, r.ToString)
            End If

            If Not String.IsNullOrEmpty(query.Hint) Then
                sb.Append(" ").Append(query.Hint)
            End If

            If query.RowNumberFilter IsNot Nothing Then
                Dim rs As String = sb.ToString
                sb.Length = 0
                sb.Append("select ")
                For Each col As String In columnAliases
                    If String.IsNullOrEmpty(col) Then
                        Throw New ExecutorException("Column alias is required")
                    End If
                    sb.Append(col).Append(",")
                Next
                sb.Length -= 1
                sb.Append(" from (").Append(rs).Append(") as t0t01 where ")
                sb.Append(query.RowNumberFilter.MakeQueryStmt(s, filterInfo, almgr, params))
            End If

            Return sb.ToString
        End Function

#End Region

        Public Function ExecEntity(Of ReturnType As {Orm._IEntity})(ByVal mgr As OrmManagerBase, ByVal query As QueryCmdBase) As ReadOnlyObjectList(Of ReturnType) Implements IExecutor.ExecEntity
            Return GetProcessorAnonym(Of ReturnType)(mgr, query).GetEntities()
        End Function

        Public Function ExecSimple(Of ReturnType)(ByVal mgr As OrmManagerBase, ByVal query As QueryCmdBase) As System.Collections.Generic.IList(Of ReturnType) Implements IExecutor.ExecSimple
            Dim ts() As Reflection.MemberInfo = Me.GetType.GetMember("ExecSimple")
            For Each t As Reflection.MethodInfo In ts
                If t.IsGenericMethod AndAlso t.GetGenericArguments.Length = 2 Then
                    t = t.MakeGenericMethod(New Type() {query.SelectedType, GetType(ReturnType)})
                    Return CType(t.Invoke(Me, New Object() {mgr, query}), System.Collections.Generic.IList(Of ReturnType))
                End If
            Next
            Throw New InvalidOperationException
        End Function

        Public Sub Reset(Of ReturnType As _ICachedEntity)(ByVal mgr As OrmManagerBase, ByVal query As QueryCmdBase) Implements IExecutor.Reset
            GetProcessor(Of ReturnType)(mgr, query).Renew = True
        End Sub

        Public Sub Reset(Of SelectType As {New, _ICachedEntity}, ReturnType As _ICachedEntity)(ByVal mgr As OrmManagerBase, ByVal query As QueryCmdBase) Implements IExecutor.Reset
            GetProcessorT(Of SelectType, ReturnType)(mgr, query).Renew = True
        End Sub
    End Class

End Namespace