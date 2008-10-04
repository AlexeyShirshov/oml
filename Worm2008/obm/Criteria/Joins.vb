Imports System.Collections.Generic

Imports Worm.Orm.Meta
Imports Worm.Criteria
Imports Worm.Criteria.Values
Imports Worm.Criteria.Core

'Imports Worm.Database.Criteria.Core

Namespace Criteria.Joins
    Public Enum JoinType
        Join
        LeftOuterJoin
        RightOuterJoin
        FullJoin
        CrossJoin
    End Enum

    Public MustInherit Class JoinFilter
        Implements Core.IFilter

        Friend _e1 As Pair(Of Type, String)
        Friend _t1 As Pair(Of SourceFragment, String)

        Friend _e2 As Pair(Of Type, String)
        Friend _t2 As Pair(Of SourceFragment, String)

        Friend _oper As FilterOperation

        Public Sub New(ByVal t As Type, ByVal fieldName As String, ByVal t2 As Type, ByVal fieldName2 As String, ByVal operation As FilterOperation)
            _e1 = New Pair(Of Type, String)(t, fieldName)
            _e2 = New Pair(Of Type, String)(t2, fieldName2)
            _oper = operation
        End Sub

        Public Sub New(ByVal table As SourceFragment, ByVal column As String, ByVal t2 As Type, ByVal FieldName2 As String, ByVal operation As FilterOperation)
            _t1 = New Pair(Of SourceFragment, String)(table, column)
            _e2 = New Pair(Of Type, String)(t2, FieldName2)
            _oper = operation
        End Sub

        Public Sub New(ByVal table As SourceFragment, ByVal column As String, ByVal table2 As SourceFragment, ByVal column2 As String, ByVal operation As FilterOperation)
            _t1 = New Pair(Of SourceFragment, String)(table, column)
            _t2 = New Pair(Of SourceFragment, String)(table2, column2)
            _oper = operation
        End Sub

        Protected Sub New()
        End Sub

        'Public Function GetStaticString() As String Implements IFilter.GetStaticString
        '    Throw New NotSupportedException
        'End Function

        Public Function ReplaceFilter(ByVal replacement As Core.IFilter, ByVal replacer As Core.IFilter) As Core.IFilter Implements Core.IFilter.ReplaceFilter
            If Not Equals(replacement) Then
                Return Nothing
            End If
            Return replacer
        End Function

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return Equals(TryCast(obj, JoinFilter))
        End Function

        Public Overloads Function Equals(ByVal obj As JoinFilter) As Boolean
            If obj Is Nothing Then
                Return False
            End If
            'Return ( _
            '    (Equals(_e1, obj._e1) AndAlso Equals(_e2, obj._e2)) OrElse (Equals(_e1, obj._e2) AndAlso Equals(_e2, obj._e1)) _
            '    ) OrElse ( _
            '    (Equals(_t1, obj._t1) AndAlso Equals(_t2, obj._t2)) OrElse (Equals(_t1, obj._t2) AndAlso Equals(_t2, obj._t1)) _
            '    )
            Dim v1 As Object = _e1
            Dim ve1 As Object = obj._e1
            If _e1 Is Nothing Then
                v1 = _t1
                ve1 = obj._t1
            End If

            Dim v2 As Object = _e2
            Dim ve2 As Object = obj._e2
            If v2 Is Nothing Then
                v2 = _t2
                ve2 = obj._e2
            End If

            Dim b As Boolean = (Equals(v1, ve1) AndAlso Equals(v2, ve2)) _
                OrElse (Equals(v1, ve2) AndAlso Equals(v2, ve1))

            Return b
        End Function

        Public Function GetAllFilters() As System.Collections.Generic.ICollection(Of Core.IFilter) Implements Core.IFilter.GetAllFilters
            Return New JoinFilter() {Me}
        End Function

        Private Function Equals1(ByVal f As Core.IFilter) As Boolean Implements Core.IFilter.Equals
            Equals(TryCast(f, JoinFilter))
        End Function

        Private Function _ToString() As String Implements Core.IFilter.ToString
            Dim sb As New StringBuilder
            If _e1 IsNot Nothing Then
                sb.Append(_e1.First.ToString).Append(_e1.Second).Append(" - ")
            Else
                sb.Append(_t1.First.RawName).Append(_t1.Second).Append(" - ")
            End If
            If _e2 IsNot Nothing Then
                sb.Append(_e2.First.ToString).Append(_e2.Second).Append(" - ")
            Else
                sb.Append(_t2.First.RawName).Append(_t2.Second).Append(" - ")
            End If
            Return sb.ToString
        End Function

        Public Overrides Function ToString() As String
            Return _ToString()
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return _ToString.GetHashCode
        End Function

        Public Function ToStaticString() As String Implements Core.IFilter.ToStaticString
            Return _ToString()
        End Function

        'Public MustOverride Function MakeQueryStmt(ByVal schema As QueryGenerator, ByVal filterInfo As Object, ByVal almgr As IPrepareTable, ByVal pname As Orm.Meta.ICreateParam) As String Implements Core.IFilter.MakeQueryStmt
        Public MustOverride Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, ByVal almgr As IPrepareTable, ByVal pname As Orm.Meta.ICreateParam) As String

        Public ReadOnly Property Filter() As Core.IFilter Implements Core.IGetFilter.Filter
            Get
                Return Me
            End Get
        End Property

        Public ReadOnly Property Filter(ByVal t As System.Type) As Core.IFilter Implements Core.IGetFilter.Filter
            Get
                Return Me
            End Get
        End Property

        Protected MustOverride Function _Clone() As Object Implements System.ICloneable.Clone

        Public Function Clone() As Core.IFilter Implements Core.IFilter.Clone
            Return CType(_Clone(), IFilter)
        End Function

        Protected Sub CopyTo(ByVal obj As JoinFilter)
            With obj
                ._e1 = _e1
                ._e2 = _e2
                ._oper = _oper
                ._t1 = _t1
                ._t2 = _t2
            End With
        End Sub

        Public Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, ByVal almgr As IPrepareTable, ByVal pname As Orm.Meta.ICreateParam, ByVal columns As System.Collections.Generic.List(Of String)) As String Implements Core.IFilter.MakeQueryStmt
            Return MakeQueryStmt(schema, filterInfo, almgr, pname)
        End Function
    End Class

    Public MustInherit Class OrmJoin
        Protected _table As SourceFragment
        Protected _joinType As Worm.Criteria.Joins.JoinType
        Protected _condition As Core.IFilter
        Protected _type As Type
        Protected _en As String

        Public Sub New(ByVal Table As SourceFragment, ByVal joinType As Worm.Criteria.Joins.JoinType, ByVal condition As Core.IFilter)
            _table = Table
            _joinType = joinType
            _condition = condition
        End Sub

        Public Sub New(ByVal type As Type, ByVal joinType As Worm.Criteria.Joins.JoinType, ByVal condition As Core.IFilter)
            _type = type
            _joinType = joinType
            _condition = condition
        End Sub

        Public Sub New(ByVal entityName As String, ByVal joinType As Worm.Criteria.Joins.JoinType, ByVal condition As Core.IFilter)
            _en = entityName
            _joinType = joinType
            _condition = condition
        End Sub

        Public Shared Function IsEmpty(ByVal j As OrmJoin) As Boolean
            Return j Is Nothing
        End Function

        'Public ReadOnly Property IsEmpty() As Boolean
        '    Get
        '        Return _table Is Nothing
        '    End Get
        'End Property

        Protected Function JoinTypeString() As String
            Select Case _joinType
                Case Worm.Criteria.Joins.JoinType.Join
                    Return " join "
                Case Worm.Criteria.Joins.JoinType.LeftOuterJoin
                    Return " left join "
                Case Worm.Criteria.Joins.JoinType.RightOuterJoin
                    Return " right join "
                Case Worm.Criteria.Joins.JoinType.FullJoin
                    Return " full join "
                Case Worm.Criteria.Joins.JoinType.CrossJoin
                    Return " cross join "
                Case Else
                    Throw New ObjectMappingException("invalid join type " & _joinType.ToString)
            End Select
        End Function

        Public Property Condition() As Core.IFilter
            Get
                Return _condition
            End Get
            Protected Friend Set(ByVal value As Core.IFilter)
                _condition = value
            End Set
        End Property

        Public Sub ReplaceFilter(ByVal replacement As Core.IFilter, ByVal replacer As Core.IFilter)
            _condition = _condition.ReplaceFilter(replacement, replacer)
        End Sub

        Public Function GetStaticString() As String
            If _table IsNot Nothing Then
                Return _table.RawName & JoinTypeString() & _condition.ToStaticString
            ElseIf _type IsNot Nothing Then
                Return _type.ToString & JoinTypeString() & _condition.ToStaticString
            Else
                Return _en & JoinTypeString() & _condition.ToStaticString
            End If
        End Function

        Public Overrides Function ToString() As String
            If _table IsNot Nothing Then
                Return _table.RawName & JoinTypeString() & _condition.ToString
            ElseIf _type IsNot Nothing Then
                Return _type.ToString & JoinTypeString() & _condition.ToString
            Else
                Return _en & JoinTypeString() & _condition.ToString
            End If
        End Function

        Public Property Type() As Type
            Get
                Return _type
            End Get
            Friend Set(ByVal value As Type)
                _type = value
            End Set
        End Property

        Public Property EntityName() As String
            Get
                Return _en
            End Get
            Friend Set(ByVal value As String)
                _en = value
            End Set
        End Property

        Public Property Table() As SourceFragment
            Get
                Return _table
            End Get
            Friend Set(ByVal value As SourceFragment)
                _table = value
            End Set
        End Property

        Public ReadOnly Property JoinType() As Worm.Criteria.Joins.JoinType
            Get
                Return _joinType
            End Get
        End Property

        Public Function InjectJoinFilter(ByVal t As Type, ByVal field As String, ByVal table As SourceFragment, ByVal column As String) As Core.TemplateBase
            For Each _fl As Core.IFilter In _condition.GetAllFilters()
                Dim f As JoinFilter = Nothing
                Dim fl As JoinFilter = TryCast(_fl, JoinFilter)
                Dim tm As Core.TemplateBase = Nothing
                If fl._e1 IsNot Nothing AndAlso fl._e1.First Is t AndAlso fl._e1.Second = field Then
                    If fl._e2 IsNot Nothing Then
                        f = CreateJoin(table, column, fl._e2.First, fl._e2.Second, fl._oper)
                        tm = CreateOrmFilter(fl._e2.First, fl._e2.Second, fl._oper)
                    Else
                        f = CreateJoin(table, column, fl._t2.First, fl._t2.Second, fl._oper)
                        tm = CreateTableFilter(fl._t2.First, fl._t2.Second, fl._oper)
                    End If
                End If
                If f Is Nothing Then
                    If fl._e2 IsNot Nothing AndAlso fl._e2.First Is t AndAlso fl._e2.Second = field Then
                        If fl._e1 IsNot Nothing Then
                            f = CreateJoin(table, column, fl._e1.First, fl._e1.Second, fl._oper)
                            tm = CreateOrmFilter(fl._e1.First, fl._e1.Second, fl._oper)
                        Else
                            f = CreateJoin(table, column, fl._t1.First, fl._t1.Second, fl._oper)
                            tm = CreateTableFilter(fl._t1.First, fl._t1.Second, fl._oper)
                        End If
                    End If
                End If

                If f IsNot Nothing Then
                    ReplaceFilter(fl, f)
                    Return tm
                End If
            Next
            Return Nothing
        End Function

        Protected MustOverride Function CreateTableFilter(ByVal table As SourceFragment, ByVal fieldName As String, ByVal oper As FilterOperation) As Core.TemplateBase
        Protected MustOverride Function CreateOrmFilter(ByVal t As Type, ByVal fieldName As String, ByVal oper As FilterOperation) As Core.TemplateBase
        Protected MustOverride Function CreateJoin(ByVal table As SourceFragment, ByVal column As String, ByVal t As Type, ByVal fieldName As String, ByVal oper As FilterOperation) As JoinFilter
        Protected MustOverride Function CreateJoin(ByVal table As SourceFragment, ByVal column As String, ByVal table2 As SourceFragment, ByVal column2 As String, ByVal oper As FilterOperation) As JoinFilter
    End Class
End Namespace

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

            Public Overrides Function MakeQueryStmt(ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, ByVal almgr As IPrepareTable, ByVal pname As Orm.Meta.ICreateParam) As String
                Dim tableAliases As System.Collections.Generic.IDictionary(Of SourceFragment, String) = almgr.Aliases

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

                Dim tableAliases As IDictionary(Of SourceFragment, String) = almgr.Aliases
                If Not tableAliases.ContainsKey(tbl) Then
                    almgr.AddTable(tbl, pname)
                End If
                'Dim table As String = _table
                'Dim sch as IOrmObjectSchema = schema.GetObjectSchema(
                Return JoinTypeString() & schema.GetTableName(tbl) & " " & tableAliases(tbl) & " on " & Condition.MakeQueryStmt(schema, filterInfo, almgr, pname, Nothing)
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

            Public Shared Function LeftJoin(ByVal t As Type) As JoinCondition
                Dim j As New OrmJoin(t, Worm.Criteria.Joins.JoinType.LeftOuterJoin, Nothing)
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

            Public Function LeftJoin(ByVal t As Type) As JoinCondition
                JC.AddFilter(_c.Condition)
                Dim j As New OrmJoin(t, Worm.Criteria.Joins.JoinType.LeftOuterJoin, Nothing)
                JC._j.Add(j)
                Return New JoinCondition(JC)
            End Function

            Public Function RightJoin(ByVal t As Type) As JoinCondition
                JC.AddFilter(_c.Condition)
                Dim j As New OrmJoin(t, Worm.Criteria.Joins.JoinType.RightOuterJoin, Nothing)
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