Imports System.Collections.Generic
Imports Worm.Entities.Meta
Imports Worm.Criteria.Joins
Imports Worm.Criteria.Core
Imports Worm.Sorting
Imports System.Collections.ObjectModel
Imports Worm.Query

Namespace Entities

    Public Interface ISelectExpressionFormater
        Sub Format(ByVal se As SelectExpression, ByVal sb As StringBuilder, ByVal executor As IExecutionContext, ByVal cols As StringBuilder, ByVal schema As ObjectMappingEngine, _
                   ByVal almgr As IPrepareTable, ByVal pmgr As ICreateParam, _
                   ByVal context As Object, ByVal selList As ObjectModel.ReadOnlyCollection(Of SelectExpression), _
                   ByVal defaultTable As SourceFragment, ByVal inSelect As Boolean)

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

        Public Sub Format(ByVal se As Entities.SelectExpression, ByVal sb As System.Text.StringBuilder, ByVal executor As IExecutionContext, _
                          ByVal cols As StringBuilder, ByVal schema As ObjectMappingEngine, ByVal almgr As IPrepareTable, ByVal pmgr As ICreateParam, _
                          ByVal context As Object, ByVal selList As ReadOnlyCollection(Of Entities.SelectExpression), _
                          ByVal defaultTable As Entities.Meta.SourceFragment, ByVal inSelect As Boolean) Implements Entities.ISelectExpressionFormater.Format
            Dim s As Sorting.Sort = TryCast(se, Sorting.Sort)
            If s IsNot Nothing Then
                Select Case se.PropType
                    Case Entities.PropType.Aggregate
                        'Dim a As Boolean = se.Aggregate.AddAlias
                        'se.Aggregate.AddAlias = False
                        sb.Append(" order by ")
                        sb.Append(se.Aggregate.MakeStmt(schema, _s, pmgr, almgr, context, False, executor))
                        If s.Order = SortType.Desc Then
                            sb.Append(" desc")
                        End If
                        'se.Aggregate.AddAlias = a
                    Case Entities.PropType.Subquery
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
                                        sb2.Append(ns.Custom.GetParam(schema, _s, Nothing, almgr, Nothing, Nothing, False, executor))
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
                                        If executor Is Nothing Then
                                            oschema = schema.GetEntitySchema(st)
                                        Else
                                            oschema = executor.GetEntitySchema(schema, st)
                                        End If

                                        If oschema Is Nothing Then
                                            Throw New SQLGeneratorException(String.Format("Object schema for field {0} of type {1} is not defined", ns.SortBy, st))
                                        End If

                                        Dim map As MapField2Column = Nothing
                                        Dim cm As Collections.IndexedCollection(Of String, MapField2Column) = oschema.GetFieldColumnMap()

                                        If cm.TryGetValue(ns.SortBy, map) Then
                                            Dim t As SourceFragment = map.Table
                                            If t Is Nothing Then
                                                t = defaultTable
                                            End If
                                            If t Is Nothing Then
                                                Throw New SQLGeneratorException(String.Format("Table for field {0} of type {1} is not defined", ns.SortBy, st))
                                            End If

                                            sb2.Append(almgr.GetAlias(t, ns.ObjectSource)).Append(_s.Selector).Append(map.Column)
                                            If ns.Order = SortType.Desc Then
                                                sb2.Append(" desc")
                                            End If
                                        Else
                                            Throw New SQLGeneratorException(String.Format("Field {0} of type {1} is not defined", ns.SortBy, st))
                                        End If
                                    Else
l1:
                                        Dim clm As String = ns.SortBy
                                        Dim tbl As SourceFragment = ns.Table
                                        If selList IsNot Nothing Then
                                            For Each p As Entities.SelectExpression In selList
                                                If p.PropertyAlias = clm AndAlso Not String.IsNullOrEmpty(p.Column) Then
                                                    If p.Table Is Nothing AndAlso tbl Is Nothing Then
                                                        clm = p.Column
                                                        'tbl = defaultTable
                                                        Exit For
                                                        'ElseIf tbl Is Nothing AndAlso defaultTable.RawName = p.Table.RawName Then
                                                        '    clm = p.Column
                                                        '    tbl = defaultTable
                                                        '    Exit For
                                                    ElseIf tbl IsNot Nothing AndAlso p.Table.RawName = tbl.RawName Then
                                                        clm = p.Column
                                                        Exit For
                                                    End If
                                                End If
                                            Next
                                            'ElseIf tbl Is Nothing Then
                                            '    tbl = defaultTable
                                        End If

                                        sb2.Append(almgr.GetAlias(tbl, Nothing)).Append(_s.Selector).Append(clm)
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
                    Case Entities.PropType.Aggregate
                        sb.Append(se.Aggregate.MakeStmt(schema, _s, pmgr, almgr, context, inSelect, executor))
                    Case Entities.PropType.TableColumn
                        Dim t As SourceFragment = se.Table
                        If t Is Nothing Then
                            t = defaultTable
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
                        If cols IsNot Nothing Then
                            If Not String.IsNullOrEmpty(al) Then
                                cols.Append(al)
                            End If
                            cols.Append(se.Column)
                        End If
                    Case Entities.PropType.ObjectProperty
                        Dim t As Type = se.ObjectSource.GetRealType(schema)
                        If t IsNot Nothing Then
                            'Dim oschema As IEntitySchema = schema.GetEntitySchema(se.ObjectSource.GetRealType(schema), False)
                            'If oschema Is Nothing Then
                            '    oschema = executor.GetEntitySchema(t)
                            'End If
                            Dim oschema As IEntitySchema
                            If executor Is Nothing Then
                                oschema = schema.GetEntitySchema(t)
                            Else
                                oschema = executor.GetEntitySchema(schema, t)
                            End If

                            Dim cm As Collections.IndexedCollection(Of String, MapField2Column) = oschema.GetFieldColumnMap()
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
                                Dim col As String = schema.GetColumnNameByPropertyAlias(oschema, se.PropertyAlias, False, se.ObjectSource)
                                sb.Append(al).Append(schema.Delimiter).Append(col)
                                If cols IsNot Nothing Then
                                    cols.Append(al).Append(schema.Delimiter).Append(col)
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

                                sb.Append(al).Append(_s.Selector).Append(map.Column)
                                If cols IsNot Nothing Then cols.Append(map.Column)
                            End If
                        Else
                            Dim tbl As SourceFragment = Query.QueryCmd.InnerTbl
                            sb.Append(tbl.UniqueName(se.ObjectSource)).Append(schema.Delimiter)
                            If String.IsNullOrEmpty(se.Column) Then
                                Dim c As String = se.ObjectSource.ObjectAlias.Query.FindColumn(schema, se.PropertyAlias)
                                If String.IsNullOrEmpty(c) Then
                                    Throw New Query.QueryCmdException(String.Format("Cannot find column {0} in inner query", se.PropertyAlias), se.ObjectSource.ObjectAlias.Query)
                                End If
                                sb.Append(If(String.IsNullOrEmpty(se.FieldAlias), se.PropertyAlias, se.FieldAlias))
                                If cols IsNot Nothing Then cols.Append(If(String.IsNullOrEmpty(se.FieldAlias), se.PropertyAlias, se.FieldAlias))
                            Else
                                sb.Append(se.Column)
                                If cols IsNot Nothing Then cols.Append(se.Column)
                            End If
                        End If

                            If Not String.IsNullOrEmpty(se.FieldAlias) AndAlso inSelect Then
                                sb.Append(" ").Append(se.FieldAlias)
                                'columnAliases.RemoveAt(columnAliases.Count - 1)
                                'columnAliases.Add(se.FieldAlias)
                            End If
                    Case Entities.PropType.CustomValue
                            If inSelect Then
                                'Dim sss As String = String.Format(se.Column, se.GetCustomExpressionValues(schema, Nothing, Nothing))
                                Dim sss As String = se.Custom.GetParam(schema, _s, pmgr, almgr, Nothing, context, inSelect, executor)
                                sb.Append(sss)
                                If cols IsNot Nothing Then cols.Append(sss)
                            Else
                                'sb.Append(String.Format(se.Column, se.GetCustomExpressionValues(schema, _s, almgr)))
                                sb.Append(se.Custom.GetParam(schema, _s, pmgr, almgr, Nothing, context, inSelect, executor))
                            End If
                            If Not String.IsNullOrEmpty(se.FieldAlias) AndAlso inSelect Then
                                sb.Append(" ").Append(se.FieldAlias)
                            End If
                    Case Entities.PropType.Subquery
                            If Not String.IsNullOrEmpty(se._tempMark) Then
                                Dim _q As Query.QueryCmd = se.Query
                                Dim c As New Query.QueryCmd.svct(_q)
                                Using New OnExitScopeAction(AddressOf c.SetCT2Nothing)
                                    Query.QueryCmd.Prepare(se.Query, Nothing, schema, context, _s)
                                    sb.Replace(se._tempMark, Query.Database.DbQueryExecutor.MakeQueryStatement(schema, context, _s, _q, pmgr, almgr))
                                End Using
                            Else
                                se._tempMark = Guid.NewGuid.ToString
                                sb.Append("(").Append(se._tempMark).Append(")")
                                If Not String.IsNullOrEmpty(se.FieldAlias) AndAlso inSelect Then
                                    sb.Append(" ").Append(se.FieldAlias)
                                End If
                            End If
                    Case Else
                            Throw New NotImplementedException(se.PropType.ToString)
                End Select
            End If

        End Sub
    End Class
End Namespace
