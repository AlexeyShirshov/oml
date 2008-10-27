Imports Worm.Orm.Meta
Imports System.Collections.Generic
Imports Worm.Criteria
Imports Worm.Criteria.Core
Imports Worm.Criteria.Values

Namespace Database
    Namespace Criteria.Joins
        Public Class JoinFilter
            Inherits Worm.Criteria.Joins.JoinFilter
            'Implements Worm.Database.Criteria.Core.IFilter

            Protected Sub New()
            End Sub

            Public Sub New(ByVal t As Type, ByVal fieldName As String, ByVal t2 As Type, ByVal fieldName2 As String, ByVal operation As FilterOperation)
                MyBase.New(t, fieldName, t2, fieldName2, operation)
            End Sub

            Public Sub New(ByVal table As SourceFragment, ByVal column As String, ByVal t As Type, ByVal fieldName As String, ByVal operation As FilterOperation)
                MyBase.New(table, column, t, fieldName, operation)
            End Sub

            Public Sub New(ByVal table As SourceFragment, ByVal column As String, ByVal table2 As SourceFragment, ByVal column2 As String, ByVal operation As FilterOperation)
                MyBase.New(table, column, table2, column2, operation)
            End Sub

            Public Sub New(ByVal t1 As Type, ByVal t2 As Type)
                MyBase.New(t1, t2, Nothing)
            End Sub

            Public Sub New(ByVal t1 As Type, ByVal t2 As Type, ByVal key As String)
                MyBase.New(t1, t2, key)
            End Sub

            Public Overrides Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, ByVal almgr As IPrepareTable, ByVal pname As Orm.Meta.ICreateParam) As String
                Dim tableAliases As System.Collections.Generic.IDictionary(Of SourceFragment, String) = almgr.Aliases

                'If _types IsNot Nothing Then
                '    Dim oschema1 As IObjectSchemaBase = schema.GetObjectSchema(_types.First)
                '    Dim oschema2 As IObjectSchemaBase = schema.GetObjectSchema(_types.Second)

                'Else
                Dim map As MapField2Column = Nothing
                If _e1 IsNot Nothing Then
                    map = schema.GetObjectSchema(_e1.First).GetFieldColumnMap(_e1.Second)
                Else
                    map = New MapField2Column(Nothing, _t1.Second, _t1.First)
                End If

                Dim map2 As MapField2Column = Nothing
                If _e2 IsNot Nothing Then
                    map2 = schema.GetObjectSchema(_e2.First).GetFieldColumnMap(_e2.Second)
                Else
                    map2 = New MapField2Column(Nothing, _t2.Second, _t2.First)
                End If

                Dim [alias] As String = String.Empty

                If tableAliases IsNot Nothing Then
                    Debug.Assert(tableAliases.ContainsKey(map._tableName), "There is not alias for table " & map._tableName.RawName)
                    Try
                        [alias] = tableAliases(map._tableName) & schema.Selector
                    Catch ex As KeyNotFoundException
                        Throw New ObjectMappingException("There is not alias for table " & map._tableName.RawName, ex)
                    End Try
                End If

                Dim alias2 As String = String.Empty
                If map2._tableName IsNot Nothing AndAlso tableAliases IsNot Nothing AndAlso tableAliases.ContainsKey(map2._tableName) Then
                    Debug.Assert(tableAliases.ContainsKey(map2._tableName), "There is not alias for table " & map2._tableName.RawName)
                    Try
                        alias2 = tableAliases(map2._tableName) & schema.Selector
                    Catch ex As KeyNotFoundException
                        Throw New ObjectMappingException("There is not alias for table " & map2._tableName.RawName, ex)
                    End Try
                End If

                Return [alias] & map._columnName & Criteria.Core.TemplateBase.Oper2String(_oper) & alias2 & map2._columnName
                'End If
            End Function

            'Public Overloads Function MakeSQLStmt(ByVal schema As DbSchema, ByVal almgr As AliasMgr, ByVal pname As ICreateParam) As String Implements Core.IFilter.MakeSQLStmt
            '    Dim tableAliases As System.Collections.Generic.IDictionary(Of SourceFragment, String) = almgr.Aliases

            '    Dim map As MapField2Column = Nothing
            '    If _e1 IsNot Nothing Then
            '        map = schema.GetObjectSchema(_e1.First).GetFieldColumnMap(_e1.Second)
            '    Else
            '        map = New MapField2Column(Nothing, _t1.Second, _t1.First)
            '    End If

            '    Dim map2 As MapField2Column = Nothing
            '    If _e2 IsNot Nothing Then
            '        map2 = schema.GetObjectSchema(_e2.First).GetFieldColumnMap(_e2.Second)
            '    Else
            '        map2 = New MapField2Column(Nothing, _t2.Second, _t2.First)
            '    End If

            '    Dim [alias] As String = String.Empty

            '    If tableAliases IsNot Nothing Then
            '        [alias] = tableAliases(map._tableName) & "."
            '    End If

            '    Dim alias2 As String = String.Empty
            '    If map2._tableName IsNot Nothing AndAlso tableAliases IsNot Nothing AndAlso tableAliases.ContainsKey(map2._tableName) Then
            '        alias2 = tableAliases(map2._tableName) & "."
            '    End If

            '    Return [alias] & map._columnName & Criteria.Core.TemplateBase.Oper2String(_oper) & alias2 & map2._columnName
            'End Function

            Protected Shared Function ChangeEntityJoinToValue(ByVal source As IFilter, ByVal t As Type, ByVal field As String, ByVal value As IParamFilterValue) As IFilter
                For Each _fl As IFilter In source.GetAllFilters()
                    Dim fl As JoinFilter = TryCast(_fl, JoinFilter)
                    If fl IsNot Nothing Then
                        Dim f As IFilter = Nothing
                        If fl._e1 IsNot Nothing AndAlso fl._e1.First Is t AndAlso fl._e1.Second = field Then
                            If fl._e2 IsNot Nothing Then
                                f = New Criteria.Core.EntityFilter(fl._e2.First, fl._e2.Second, value, fl._oper)
                            Else
                                f = New Criteria.Core.TableFilter(fl._t2.First, fl._t2.Second, value, fl._oper)
                            End If
                        ElseIf fl._e2 IsNot Nothing AndAlso fl._e2.First Is t AndAlso fl._e2.Second = field Then
                            If fl._e1 IsNot Nothing Then
                                f = New Criteria.Core.EntityFilter(fl._e1.First, fl._e1.Second, value, fl._oper)
                            Else
                                f = New Criteria.Core.TableFilter(fl._t1.First, fl._t1.Second, value, fl._oper)
                            End If
                        End If

                        If f IsNot Nothing Then
                            Return CType(source.ReplaceFilter(fl, f), IFilter)
                        End If
                    End If
                Next
                Return Nothing
            End Function

            Public Shared Function ChangeEntityJoinToLiteral(ByVal source As IFilter, ByVal t As Type, ByVal field As String, ByVal literal As String) As IFilter
                Return ChangeEntityJoinToValue(source, t, field, New LiteralValue(literal))
            End Function

            Public Shared Function ChangeEntityJoinToParam(ByVal source As IFilter, ByVal t As Type, ByVal field As String, ByVal value As TypeWrap(Of Object)) As IFilter
                Return ChangeEntityJoinToValue(source, t, field, New ScalarValue(value.Value))
            End Function

            Protected Overrides Function _Clone() As Object
                Dim c As New JoinFilter
                CopyTo(c)
                Return c
            End Function
        End Class

        Public Class OrmJoin
            Inherits Worm.Criteria.Joins.OrmJoin

            Public Sub New(ByVal table As SourceFragment, ByVal joinType As Worm.Criteria.Joins.JoinType, ByVal condition As IFilter)
                MyBase.New(table, joinType, condition)
            End Sub

            Public Sub New(ByVal t As Type, ByVal joinType As Worm.Criteria.Joins.JoinType, ByVal condition As IFilter)
                MyBase.New(t, joinType, condition)
            End Sub

            Public Sub New(ByVal entityName As String, ByVal joinType As Worm.Criteria.Joins.JoinType, ByVal condition As IFilter)
                MyBase.New(entityName, joinType, condition)
            End Sub

            Public Function MakeSQLStmt(ByVal schema As SQLGenerator, ByVal filterInfo As Object, ByVal almgr As IPrepareTable, ByVal pname As ICreateParam) As String
                'If IsEmpty Then
                '    Throw New InvalidOperationException("Object must be created")
                'End If

                If Condition Is Nothing Then
                    Throw New InvalidOperationException("Join condition must be specified")
                End If

                If almgr Is Nothing Then
                    Throw New ArgumentNullException("almgr")
                End If

                Dim tbl As SourceFragment = _table
                If tbl Is Nothing Then
                    If _type IsNot Nothing Then
                        tbl = schema.GetTables(_type)(0)
                    Else
                        tbl = schema.GetTables(schema.GetTypeByEntityName(_en))(0)
                    End If
                End If

                Dim alTable As SourceFragment = tbl
                Dim tableAliases As IDictionary(Of SourceFragment, String) = almgr.Aliases
                If Not tableAliases.ContainsKey(tbl) Then
                    almgr.AddTable(tbl, pname)
                End If
                'Dim table As String = _table
                'Dim sch as IOrmObjectSchema = schema.GetObjectSchema(
                Return JoinTypeString() & schema.GetTableName(tbl) & " " & tableAliases(alTable) & " on " & Condition.MakeQueryStmt(schema, filterInfo, almgr, pname, Nothing)
            End Function

            Protected Overrides Function CreateOrmFilter(ByVal t As System.Type, ByVal fieldName As String, ByVal oper As FilterOperation) As Worm.Criteria.Core.TemplateBase
                Return New Core.OrmFilterTemplate(t, fieldName, oper)
            End Function

            Protected Overrides Function CreateTableFilter(ByVal table As Orm.Meta.SourceFragment, ByVal fieldName As String, ByVal oper As FilterOperation) As Worm.Criteria.Core.TemplateBase
                Return New Core.TableFilterTemplate(table, fieldName, oper)
            End Function

            'Public Overloads ReadOnly Property Condition() As IFilter
            '    Get
            '        Return CType(_condition, IFilter)
            '    End Get
            'End Property

            Protected Overloads Overrides Function CreateJoin(ByVal table As Orm.Meta.SourceFragment, ByVal column As String, ByVal t As System.Type, ByVal fieldName As String, ByVal oper As Worm.Criteria.FilterOperation) As Worm.Criteria.Joins.JoinFilter
                Return New JoinFilter(table, column, t, fieldName, oper)
            End Function

            Protected Overloads Overrides Function CreateJoin(ByVal table As Orm.Meta.SourceFragment, ByVal column As String, ByVal table2 As Orm.Meta.SourceFragment, ByVal column2 As String, ByVal oper As Worm.Criteria.FilterOperation) As Worm.Criteria.Joins.JoinFilter
                Return New JoinFilter(table, column, table2, column2, oper)
            End Function
        End Class

        Public Class JCtor
            Friend _j As New List(Of OrmJoin)

            Public Shared Function Join(ByVal t As Type) As JoinCondition
                Dim j As New OrmJoin(t, Worm.Criteria.Joins.JoinType.Join, Nothing)
                Dim jc As New JCtor
                jc._j.Add(j)
                Return New JoinCondition(jc)
            End Function

            Public Shared Function Join(ByVal entityName As String) As JoinCondition
                Dim j As New OrmJoin(entityName, Worm.Criteria.Joins.JoinType.Join, Nothing)
                Dim jc As New JCtor
                jc._j.Add(j)
                Return New JoinCondition(jc)
            End Function

            Public Shared Function LeftJoin(ByVal t As Type) As JoinCondition
                Dim j As New OrmJoin(t, Worm.Criteria.Joins.JoinType.LeftOuterJoin, Nothing)
                Dim jc As New JCtor
                jc._j.Add(j)
                Return New JoinCondition(jc)
            End Function

            Public Shared Function LeftJoin(ByVal entityName As String) As JoinCondition
                Dim j As New OrmJoin(entityName, Worm.Criteria.Joins.JoinType.LeftOuterJoin, Nothing)
                Dim jc As New JCtor
                jc._j.Add(j)
                Return New JoinCondition(jc)
            End Function

            Public Shared Function RightJoin(ByVal t As Type) As JoinCondition
                Dim j As New OrmJoin(t, Worm.Criteria.Joins.JoinType.RightOuterJoin, Nothing)
                Dim jc As New JCtor
                jc._j.Add(j)
                Return New JoinCondition(jc)
            End Function

            Public Shared Function RightJoin(ByVal entityName As String) As JoinCondition
                Dim j As New OrmJoin(entityName, Worm.Criteria.Joins.JoinType.RightOuterJoin, Nothing)
                Dim jc As New JCtor
                jc._j.Add(j)
                Return New JoinCondition(jc)
            End Function

            Public Shared Function Join(ByVal table As SourceFragment) As JoinCondition
                Dim j As New OrmJoin(table, Worm.Criteria.Joins.JoinType.Join, Nothing)
                Dim jc As New JCtor
                jc._j.Add(j)
                Return New JoinCondition(jc)
            End Function

            Public Shared Function LeftJoin(ByVal table As SourceFragment) As JoinCondition
                Dim j As New OrmJoin(table, Worm.Criteria.Joins.JoinType.LeftOuterJoin, Nothing)
                Dim jc As New JCtor
                jc._j.Add(j)
                Return New JoinCondition(jc)
            End Function

            Public Shared Function RightJoin(ByVal table As SourceFragment) As JoinCondition
                Dim j As New OrmJoin(table, Worm.Criteria.Joins.JoinType.RightOuterJoin, Nothing)
                Dim jc As New JCtor
                jc._j.Add(j)
                Return New JoinCondition(jc)
            End Function

            'Public Function AddJoin(ByVal t As Type) As JoinCondition
            '    Dim j As New OrmJoin(t, Worm.Criteria.Joins.JoinType.Join, Nothing)
            '    _j.Add(j)
            '    Return New JoinCondition(Me)
            'End Function

            'Public Function AddLeftJoin(ByVal t As Type) As JoinCondition
            '    Dim j As New OrmJoin(t, Worm.Criteria.Joins.JoinType.LeftOuterJoin, Nothing)
            '    _j.Add(j)
            '    Return New JoinCondition(Me)
            'End Function

            'Public Function AddRightJoin(ByVal t As Type) As JoinCondition
            '    Dim j As New OrmJoin(t, Worm.Criteria.Joins.JoinType.RightOuterJoin, Nothing)
            '    Dim jc As New JCtor
            '    _j.Add(j)
            '    Return New JoinCondition(Me)
            'End Function

            'Public Function AddJoin(ByVal table As SourceFragment) As JoinCondition
            '    Dim j As New OrmJoin(table, Worm.Criteria.Joins.JoinType.Join, Nothing)
            '    Dim jc As New JCtor
            '    _j.Add(j)
            '    Return New JoinCondition(Me)
            'End Function

            'Public Function AddLeftJoin(ByVal table As SourceFragment) As JoinCondition
            '    Dim j As New OrmJoin(table, Worm.Criteria.Joins.JoinType.LeftOuterJoin, Nothing)
            '    Dim jc As New JCtor
            '    _j.Add(j)
            '    Return New JoinCondition(Me)
            'End Function

            'Public Function AddRightJoin(ByVal table As SourceFragment) As JoinCondition
            '    Dim j As New OrmJoin(table, Worm.Criteria.Joins.JoinType.RightOuterJoin, Nothing)
            '    Dim jc As New JCtor
            '    _j.Add(j)
            '    Return New JoinCondition(Me)
            'End Function

            Protected Friend Function AddFilter(ByVal jf As IFilter) As JCtor
                _j(_j.Count - 1).Condition = jf
                Return Me
            End Function

            Public Function Joins() As IEnumerable(Of OrmJoin)
                Return _j
            End Function

            Public Function ToJoinArray() As OrmJoin()
                Return _j.ToArray
            End Function
        End Class

        Public Class JoinLink
            Implements IGetFilter

            Private _c As Conditions.Condition.ConditionConstructor
            Private _jc As JCtor

            Protected Friend Sub New(ByVal f As IFilter, ByVal jc As JCtor)
                _c = New Conditions.Condition.ConditionConstructor
                _c.AddFilter(f)
                _jc = jc
            End Sub

            Protected Friend Sub New(ByVal c As Conditions.Condition.ConditionConstructor, ByVal jc As JCtor)
                _c = c
                _jc = jc
            End Sub

            Public Function [And](ByVal f As IGetFilter) As JoinLink
                _c.AddFilter(f.Filter, Worm.Criteria.Conditions.ConditionOperator.And)
                Return Me
            End Function

            Public Function [And](ByVal t As Type, ByVal field As String) As CriteriaJoin
                Dim jf As New JoinFilter(t, field, CType(Nothing, Type), Nothing, FilterOperation.Equal)
                Dim c As New CriteriaJoin(jf, _c, _jc)
                Return c
            End Function

            Public Function [And](ByVal table As SourceFragment, ByVal column As String) As CriteriaJoin
                Dim jf As New JoinFilter(table, column, CType(Nothing, Type), Nothing, FilterOperation.Equal)
                Dim c As New CriteriaJoin(jf, _c, _jc)
                Return c
            End Function

            Public ReadOnly Property Condition() As IFilter
                Get
                    Return _c.Condition
                End Get
            End Property

            Protected ReadOnly Property JC() As JCtor
                Get
                    Return _jc
                End Get
            End Property

            Public Function Join(ByVal t As Type) As JoinCondition
                JC.AddFilter(_c.Condition)
                Dim j As New OrmJoin(t, Worm.Criteria.Joins.JoinType.Join, Nothing)
                JC._j.Add(j)
                Return New JoinCondition(JC)
            End Function

            Public Function Join(ByVal entityName As String) As JoinCondition
                JC.AddFilter(_c.Condition)
                Dim j As New OrmJoin(entityName, Worm.Criteria.Joins.JoinType.Join, Nothing)
                JC._j.Add(j)
                Return New JoinCondition(JC)
            End Function

            Public Function LeftJoin(ByVal t As Type) As JoinCondition
                JC.AddFilter(_c.Condition)
                Dim j As New OrmJoin(t, Worm.Criteria.Joins.JoinType.LeftOuterJoin, Nothing)
                JC._j.Add(j)
                Return New JoinCondition(JC)
            End Function

            Public Function LeftJoin(ByVal entityName As String) As JoinCondition
                JC.AddFilter(_c.Condition)
                Dim j As New OrmJoin(entityName, Worm.Criteria.Joins.JoinType.LeftOuterJoin, Nothing)
                JC._j.Add(j)
                Return New JoinCondition(JC)
            End Function

            Public Function RightJoin(ByVal t As Type) As JoinCondition
                JC.AddFilter(_c.Condition)
                Dim j As New OrmJoin(t, Worm.Criteria.Joins.JoinType.RightOuterJoin, Nothing)
                JC._j.Add(j)
                Return New JoinCondition(JC)
            End Function

            Public Function RightJoin(ByVal entityName As String) As JoinCondition
                JC.AddFilter(_c.Condition)
                Dim j As New OrmJoin(entityName, Worm.Criteria.Joins.JoinType.RightOuterJoin, Nothing)
                JC._j.Add(j)
                Return New JoinCondition(JC)
            End Function

            Public Function Join(ByVal table As SourceFragment) As JoinCondition
                JC.AddFilter(_c.Condition)
                Dim j As New OrmJoin(table, Worm.Criteria.Joins.JoinType.Join, Nothing)
                JC._j.Add(j)
                Return New JoinCondition(JC)
            End Function

            Public Function LeftJoin(ByVal table As SourceFragment) As JoinCondition
                JC.AddFilter(_c.Condition)
                Dim j As New OrmJoin(table, Worm.Criteria.Joins.JoinType.LeftOuterJoin, Nothing)
                JC._j.Add(j)
                Return New JoinCondition(JC)
            End Function

            Public Function RightJoin(ByVal table As SourceFragment) As JoinCondition
                JC.AddFilter(_c.Condition)
                Dim j As New OrmJoin(table, Worm.Criteria.Joins.JoinType.RightOuterJoin, Nothing)
                JC._j.Add(j)
                Return New JoinCondition(JC)
            End Function

            Public Shared Widening Operator CType(ByVal jl As JoinLink) As OrmJoin()
                Return jl.JC.AddFilter(jl._c.Condition).ToJoinArray
            End Operator

            Public ReadOnly Property Filter() As IFilter Implements IGetFilter.Filter
                Get
                    Return Condition
                End Get
            End Property

            Public ReadOnly Property Filter(ByVal t As System.Type) As IFilter Implements IGetFilter.Filter
                Get
                    Return Condition
                End Get
            End Property
        End Class

        Public Class CriteriaJoin
            Private _jf As JoinFilter
            Private _c As Conditions.Condition.ConditionConstructor
            Private _jc As JCtor

            Protected Friend Sub New(ByVal jf As JoinFilter, ByVal jc As JCtor)
                _jf = jf
                _jc = jc
            End Sub

            Protected Friend Sub New(ByVal jf As JoinFilter, ByVal c As Conditions.Condition.ConditionConstructor, ByVal jc As JCtor)
                _jf = jf
                _c = c
                _jc = jc
            End Sub

            Public Function Eq(ByVal t As Type, ByVal field As String) As JoinLink
                _jf._e2 = New Pair(Of Type, String)(t, field)
                _jf._oper = FilterOperation.Equal
                Return GetLink()
            End Function

            Public Function Eq(ByVal table As SourceFragment, ByVal column As String) As JoinLink
                _jf._t2 = New Pair(Of SourceFragment, String)(table, column)
                _jf._oper = FilterOperation.Equal
                Return GetLink()
            End Function

            Protected Function GetLink() As JoinLink
                If _c Is Nothing Then
                    _c = New Conditions.Condition.ConditionConstructor
                End If
                _c.AddFilter(_jf)
                Return New JoinLink(_c, _jc)
            End Function
        End Class

        Public Class JoinCondition
            Private _j As JCtor

            Public Sub New(ByVal jc As JCtor)
                _j = jc
            End Sub

            Public Function [On](ByVal f As IFilter) As JoinLink
                Return New JoinLink(f, _j)
            End Function

            Public Function [On](ByVal t As Type, ByVal field As String) As CriteriaJoin
                Dim jf As New JoinFilter(t, field, CType(Nothing, Type), Nothing, FilterOperation.Equal)
                Dim c As New CriteriaJoin(jf, _j)
                Return c
            End Function

            Public Function [On](ByVal table As SourceFragment, ByVal column As String) As CriteriaJoin
                Dim jf As New JoinFilter(table, column, CType(Nothing, Type), Nothing, FilterOperation.Equal)
                Dim c As New CriteriaJoin(jf, _j)
                Return c
            End Function

            Public Shared Function Create(ByVal t As Type, ByVal field As String) As CriteriaJoin
                Dim jf As New JoinFilter(t, field, CType(Nothing, Type), Nothing, FilterOperation.Equal)
                Dim c As New CriteriaJoin(jf, Nothing)
                Return c
            End Function

            Public Shared Function Create(ByVal table As SourceFragment, ByVal column As String) As CriteriaJoin
                Dim jf As New JoinFilter(table, column, CType(Nothing, Type), Nothing, FilterOperation.Equal)
                Dim c As New CriteriaJoin(jf, Nothing)
                Return c
            End Function
        End Class
    End Namespace
End Namespace