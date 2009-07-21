Imports System.Collections.Generic
Imports Worm.Entities.Meta
Imports Worm.Criteria.Joins
Imports Worm.Criteria.Core
Imports Worm.Query.Sorting
Imports System.Collections.ObjectModel
Imports Worm.Query

#If kdkdkdk Then
Namespace Entities

    Public Interface ISelectExpressionFormater
        Sub Format(ByVal se As SelectExpressionOld, ByVal sb As StringBuilder, ByVal executor As IExecutionContext, ByVal cols As StringBuilder, ByVal schema As ObjectMappingEngine, _
                   ByVal almgr As IPrepareTable, ByVal pmgr As ICreateParam, _
                   ByVal context As Object, ByVal selList As ObjectModel.ReadOnlyCollection(Of SelectExpressionOld), _
                   ByVal defaultTable As QueryCmd.FromClauseDef, ByVal inSelect As Boolean)

    End Interface

    <Serializable()> _
    Public Class SelectFormaterException
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

Namespace Database
    Public Class SelectExpressionFormater
        Implements Entities.ISelectExpressionFormater

        Private _s As SQLGenerator

        Public Sub New(ByVal s As SQLGenerator)
            _s = s
        End Sub

        Public Sub Format(ByVal se As SelectExpressionOld, ByVal sb As System.Text.StringBuilder, ByVal executor As IExecutionContext, _
                          ByVal cols As StringBuilder, ByVal schema As ObjectMappingEngine, ByVal almgr As IPrepareTable, ByVal pmgr As ICreateParam, _
                          ByVal context As Object, ByVal selList As ReadOnlyCollection(Of SelectExpressionOld), _
                          ByVal defaultTable As QueryCmd.FromClauseDef, ByVal inSelect As Boolean) Implements Entities.ISelectExpressionFormater.Format
            Dim s As Sorting.Sort = TryCast(se, Sorting.Sort)
            If s IsNot Nothing Then
                Select Case se.PropType
                    Case PropType.Aggregate
                        'Dim a As Boolean = se.Aggregate.AddAlias
                        'se.Aggregate.AddAlias = False
                        sb.Append(" order by ")
                        sb.Append(se.Aggregate.MakeStmt(schema, defaultTable, _s, pmgr, almgr, context, False, executor))
                        If s.Order = SortType.Desc Then
                            sb.Append(" desc")
                        End If
                        'se.Aggregate.AddAlias = a
                    Case PropType.Subquery
                        Dim _q As Query.QueryCmd = se.Query

                        Dim c As New Query.QueryCmd.svct(_q)
                        Using New OnExitScopeAction(AddressOf c.SetCT2Nothing)
                            'If _q.SelectedType Is Nothing Then
                            '    If String.IsNullOrEmpty(_q.SelectedEntityName) Then
                            '        _q.SelectedType = _q.CreateType
                            '    Else
                            '        _q.SelectedType = schema.GetTypeByEntityName(_q.SelectedEntityName)
                            '    End If
                            'End If

                            'If GetType(Entities.AnonymousEntity).IsAssignableFrom(_q.SelectedType) Then
                            '    _q.SelectedType = Nothing
                            'End If

                            'If _q.CreateType Is Nothing AndAlso _q.SelectedType IsNot Nothing Then
                            '    _q.Into(_q.SelectedType)
                            'End If

                            Query.QueryCmd.Prepare(se.Query, Nothing, schema, context, _s)
                            sb.Append(" order by (")
                            sb.Append(Query.Database.DbQueryExecutor.MakeQueryStatement(schema, context, _s, _q, pmgr, almgr))
                        End Using
                        sb.Append(")")
                        If s.Order = SortType.Desc Then
                            sb.Append(" desc")
                        End If
                    Case Else
                        '_s.AppendOrder(schema, s, almgr, sb, True, selList, defaultTable, defaultObjectSchema)
                        If s IsNot Nothing AndAlso Not s.IsExternal Then 'AndAlso Not sort.IsAny
                            'If appendOrder Then
                            sb.Append(" order by ")
                            'End If
                            Dim pos As Integer = sb.Length
                            For Each ns As Sort In New Sort.Iterator(s)
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
                                        'sb2.Append(String.Format(ns.CustomSortExpression, ns.GetCustomExpressionValues(mpe, Me, almgr)))
                                        sb2.Append(ns.Custom.GetParam(schema, defaultTable, _s, Nothing, almgr, Nothing, Nothing, False, executor))
                                    Else
                                        sb2.Append(ns.CustomSortExpression)
                                    End If
                                    If ns.Order = SortType.Desc Then
                                        sb2.Append(" desc")
                                    End If
                                Else
                                    Dim st As Type = Nothing
                                    If ns.ObjectSource IsNot Nothing Then
                                        st = ns.ObjectSource.GetRealType(schema)
                                    End If

                                    If st IsNot Nothing Then
                                        'Dim oschema As IEntitySchema = CType(schema.GetEntitySchema(st, False), IEntitySchema)

                                        'If oschema Is Nothing Then
                                        '    oschema = executor.GetEntitySchema(st)
                                        'End If
                                        Dim oschema As IEntitySchema
                                        Dim cm As Collections.IndexedCollection(Of String, MapField2Column)
                                        If executor Is Nothing Then
                                            oschema = schema.GetEntitySchema(st)
                                            cm = oschema.GetFieldColumnMap
                                        Else
                                            oschema = executor.GetEntitySchema(schema, st)
                                            cm = executor.GetFieldColumnMap(oschema, st)
                                        End If

                                        If oschema Is Nothing Then
                                            Throw New SQLGeneratorException(String.Format("Object schema for field {0} of type {1} is not defined", ns.SortBy, st))
                                        End If

                                        Dim map As MapField2Column = Nothing

                                        If cm.TryGetValue(ns.SortBy, map) Then
                                            Dim t As SourceFragment = map.Table
                                            If t Is Nothing Then
                                                t = defaultTable.Table
                                            End If
                                            If t Is Nothing Then
                                                Throw New SQLGeneratorException(String.Format("Table for field {0} of type {1} is not defined", ns.SortBy, st))
                                            End If

                                            sb2.Append(almgr.GetAlias(t, ns.ObjectSource)).Append(_s.Selector).Append(map.ColumnExpression)
                                            If ns.Order = SortType.Desc Then
                                                sb2.Append(" desc")
                                            End If
                                        Else
                                            Throw New SQLGeneratorException(String.Format("Field {0} of type {1} is not defined", ns.SortBy, st))
                                        End If
                                    Else
l1:
                                        Dim clm As String = ns.SortBy
                                        If selList IsNot Nothing Then
                                            For Each p As SelectExpressionOld In selList
                                                If p.PropertyAlias = clm Then
                                                    clm = If(String.IsNullOrEmpty(p.ColumnAlias), p.Column, p.ColumnAlias)
                                                    Exit For
                                                End If
                                            Next
                                        End If
                                        If String.IsNullOrEmpty(clm) Then
                                            clm = defaultTable.AnyQuery.FindColumn(schema, ns.PropertyAlias)
                                        End If

                                        Dim tbl As SourceFragment = ns.Table
                                        If tbl Is Nothing Then
                                            tbl = defaultTable.QueryEU.ObjectAlias.Tbl
                                            If tbl Is Nothing Then
                                                tbl = New SourceFragment
                                                defaultTable.QueryEU.ObjectAlias.Tbl = tbl
                                            End If
                                        End If

                                        sb2.Append(almgr.GetAlias(tbl, defaultTable.QueryEU)).Append(_s.Selector).Append(clm)
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
                End Select
            Else
                Select Case se.PropType
                    Case PropType.Aggregate
                        sb.Append(se.Aggregate.MakeStmt(schema, defaultTable, _s, pmgr, almgr, context, inSelect, executor))
                        If Not String.IsNullOrEmpty(se.ColumnAlias) AndAlso inSelect Then
                            sb.Append(" ").Append(se.ColumnAlias)
                        End If
                    Case PropType.TableColumn
                        Dim t As SourceFragment = se.Table
                        If t Is Nothing Then
                            t = defaultTable.Table
                        End If
                        If t Is Nothing Then
                            Throw New Entities.SelectFormaterException("Table is not specified for column " & se.Column)
                        End If
                        Dim al As String = Nothing
                        If Not String.IsNullOrEmpty(t.Name) Then
                            If inSelect Then
                                al = t.UniqueName(se.ObjectSource) & schema.Delimiter
                            Else
                                al = almgr.GetAlias(se.Table, Nothing) & _s.Selector
                            End If
                        End If
                        If Not String.IsNullOrEmpty(al) Then
                            sb.Append(al)
                        End If
                        sb.Append(se.Column)
                        If Not String.IsNullOrEmpty(se.ColumnAlias) Then
                            sb.Append(" ").Append(se.ColumnAlias)
                        End If
                        If cols IsNot Nothing Then
                            If Not String.IsNullOrEmpty(al) Then
                                cols.Append(al)
                            End If
                            If Not String.IsNullOrEmpty(se.ColumnAlias) Then
                                cols.Append(se.ColumnAlias)
                            Else
                                cols.Append(se.Column)
                            End If
                        End If
                    Case PropType.ObjectProperty
                        If se.ObjectSource IsNot Nothing Then
                            Dim t As Type = se.ObjectSource.GetRealType(schema)
                            If t Is Nothing Then
                                GoTo l2
                            End If
                            'Dim oschema As IEntitySchema = schema.GetEntitySchema(se.ObjectSource.GetRealType(schema), False)
                            'If oschema Is Nothing Then
                            '    oschema = executor.GetEntitySchema(t)
                            'End If
                            Dim oschema As IEntitySchema
                            Dim cm As Collections.IndexedCollection(Of String, MapField2Column)
                            If executor Is Nothing Then
                                oschema = schema.GetEntitySchema(t)
                                cm = oschema.GetFieldColumnMap
                            Else
                                oschema = executor.GetEntitySchema(schema, t)
                                cm = executor.GetFieldColumnMap(oschema, t)
                            End If

                            Dim map As MapField2Column = cm(se.PropertyAlias)
                            If inSelect Then
                                Dim al As String = Nothing
                                If se.ObjectSource.IsQuery Then
                                    Dim tbl As SourceFragment = se.ObjectSource.ObjectAlias.Tbl
                                    If tbl Is Nothing Then
                                        tbl = New SourceFragment
                                        se.ObjectSource.ObjectAlias.Tbl = tbl
                                    End If
                                    al = tbl.UniqueName(se.ObjectSource)
                                Else
                                    al = map.Table.UniqueName(se.ObjectSource)
                                End If
                                'Dim col As EntityPropertyAttribute = schema.GetColumnByPropertyAlias(t, se.PropertyAlias, oschema)
                                Dim colExp As String = map.ColumnExpression 'col.Column
                                Dim colName As String = map.ColumnName 'col.ColumnName
                                sb.Append(al).Append(schema.Delimiter).Append(colExp)
                                If se.AddAlias Then
                                    If Not String.IsNullOrEmpty(colName) AndAlso String.IsNullOrEmpty(se.ColumnAlias) Then
                                        sb.Append(" as ").Append(colName)
                                    End If
                                End If
                                If cols IsNot Nothing Then
                                    cols.Append(al).Append(schema.Delimiter)
                                    If Not String.IsNullOrEmpty(colName) Then
                                        cols.Append(colName)
                                    Else
                                        cols.Append(colExp)
                                    End If
                                End If
                            Else
                                Dim al As String = Nothing
                                If se.ObjectSource.IsQuery Then
                                    Dim tbl As SourceFragment = se.ObjectSource.ObjectAlias.Tbl
                                    If tbl Is Nothing Then
                                        tbl = New SourceFragment
                                        se.ObjectSource.ObjectAlias.Tbl = tbl
                                    End If
                                    al = almgr.GetAlias(tbl, se.ObjectSource)
                                Else
                                    If cm.TryGetValue(se.PropertyAlias, map) Then
                                        al = almgr.GetAlias(map.Table, se.ObjectSource)
                                    Else
                                        Throw New ArgumentException(String.Format("Field {0} of type {1} is not defined", se.PropertyAlias, se.ObjectSource.ToStaticString(schema, context)))
                                    End If
                                End If

                                sb.Append(al).Append(_s.Selector).Append(map.ColumnExpression)
                                'If Not String.IsNullOrEmpty(map.ColumnName) AndAlso String.IsNullOrEmpty(se.ColumnAlias) Then
                                '    sb.Append(" as ").Append(map.ColumnName)
                                'End If
                                If cols IsNot Nothing Then
                                    If Not String.IsNullOrEmpty(map.ColumnName) Then
                                        cols.Append(map.ColumnName)
                                    Else
                                        cols.Append(map.ColumnExpression)
                                    End If
                                End If
                            End If
                        Else
l2:
                            Dim eu As EntityUnion = defaultTable.QueryEU

                            If eu Is Nothing Then
                                Throw New InvalidOperationException("Property " & se.PropertyAlias & " has not source")
                            End If
                            Dim tbl As SourceFragment = eu.ObjectAlias.Tbl
                            If tbl Is Nothing Then
                                tbl = New SourceFragment
                                eu.ObjectAlias.Tbl = tbl
                            End If
                            If inSelect Then
                                sb.Append(tbl.UniqueName(eu)).Append(schema.Delimiter)
                            Else
                                sb.Append(almgr.GetAlias(tbl, eu)).Append(_s.Selector)
                            End If
                            If String.IsNullOrEmpty(se.Column) Then
                                Dim q As QueryCmd = Nothing
                                If se.ObjectSource IsNot Nothing Then
                                    q = se.ObjectSource.ObjectAlias.Query
                                Else
                                    q = defaultTable.AnyQuery
                                End If
                                Dim c As String = q.FindColumn(schema, se.PropertyAlias)
                                If String.IsNullOrEmpty(c) Then
                                    Throw New Query.QueryCmdException(String.Format("Cannot find column {0} in inner query", se.PropertyAlias), q)
                                End If
                                'sb.Append(If(String.IsNullOrEmpty(se.FieldAlias), se.PropertyAlias, se.FieldAlias))
                                'If cols IsNot Nothing Then cols.Append(If(String.IsNullOrEmpty(se.FieldAlias), se.PropertyAlias, se.FieldAlias))
                                sb.Append(c)
                                If cols IsNot Nothing Then cols.Append(c)
                            Else
                                sb.Append(se.Column)
                                If cols IsNot Nothing Then cols.Append(se.Column)
                            End If
                        End If

                        If Not String.IsNullOrEmpty(se.ColumnAlias) AndAlso inSelect Then
                            sb.Append(" ").Append(se.ColumnAlias)
                            'columnAliases.RemoveAt(columnAliases.Count - 1)
                            'columnAliases.Add(se.FieldAlias)
                        End If
                    Case PropType.CustomValue
                        If inSelect Then
                            'Dim sss As String = String.Format(se.Column, se.GetCustomExpressionValues(schema, Nothing, Nothing))
                            Dim sss As String = se.Custom.GetParam(schema, defaultTable, _s, pmgr, almgr, Nothing, context, inSelect, executor)
                            sb.Append(sss)
                            If cols IsNot Nothing Then cols.Append(sss)
                        Else
                            'sb.Append(String.Format(se.Column, se.GetCustomExpressionValues(schema, _s, almgr)))
                            sb.Append(se.Custom.GetParam(schema, defaultTable, _s, pmgr, almgr, Nothing, context, inSelect, executor))
                        End If
                        If Not String.IsNullOrEmpty(se.ColumnAlias) AndAlso inSelect Then
                            sb.Append(" ").Append(se.ColumnAlias)
                        End If
                    Case PropType.Subquery
                        If Not String.IsNullOrEmpty(se._tempMark) Then
                            Dim _q As Query.QueryCmd = se.Query
                            'Dim c As New Query.QueryCmd.svct(_q)
                            'Using New OnExitScopeAction(AddressOf c.SetCT2Nothing)
                            '    Query.QueryCmd.Prepare(se.Query, Nothing, schema, context, _s)
                            sb.Replace(se._tempMark, Query.Database.DbQueryExecutor.MakeQueryStatement(schema, context, _s, _q, pmgr, almgr))
                            'End Using
                        Else
                            se._tempMark = Guid.NewGuid.ToString
                            sb.Append("(").Append(se._tempMark).Append(")")
                            If Not String.IsNullOrEmpty(se.ColumnAlias) AndAlso inSelect Then
                                sb.Append(" ").Append(se.ColumnAlias)
                            End If
                        End If
                    Case Else
                        Throw New NotImplementedException(se.PropType.ToString)
                End Select
            End If

        End Sub
    End Class
End Namespace

#End If