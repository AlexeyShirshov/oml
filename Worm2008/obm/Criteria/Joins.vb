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
            Dim p As Pair(Of Type, String) = Nothing
            If t IsNot Nothing Then
                p = New Pair(Of Type, String)(t, fieldName)
            End If
            _e1 = p

            p = Nothing
            If t2 IsNot Nothing Then
                p = New Pair(Of Type, String)(t2, fieldName2)
            End If
            _e2 = p

            _oper = operation
        End Sub

        Public Sub New(ByVal table As SourceFragment, ByVal column As String, ByVal t2 As Type, ByVal fieldName2 As String, ByVal operation As FilterOperation)
            Dim t As Pair(Of SourceFragment, String) = Nothing
            If table IsNot Nothing Then
                t = New Pair(Of SourceFragment, String)(table, column)
            End If
            _t1 = t

            Dim p As Pair(Of Type, String) = Nothing
            If t2 IsNot Nothing Then
                p = New Pair(Of Type, String)(t2, fieldName2)
            End If
            _e2 = p

            _oper = operation
        End Sub

        Public Sub New(ByVal table As SourceFragment, ByVal column As String, ByVal table2 As SourceFragment, ByVal column2 As String, ByVal operation As FilterOperation)
            Dim t As Pair(Of SourceFragment, String) = Nothing
            If table IsNot Nothing Then
                t = New Pair(Of SourceFragment, String)(table, column)
            End If
            _t1 = t

            t = Nothing
            If table2 IsNot Nothing Then
                t = New Pair(Of SourceFragment, String)(table2, column2)
            End If
            _t2 = t

            _oper = operation
        End Sub

        'Public Sub New(ByVal t1 As Type, ByVal t2 As Type)
        '    MyClass.New(t1, t2, Nothing)
        'End Sub

        'Public Sub New(ByVal t1 As Type, ByVal t2 As Type, ByVal key As String)
        '    _types = New Pair(Of Type)(t1, t2)
        '    _key = key
        'End Sub

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

        Private Function _ToString() As String Implements Core.IFilter._ToString
            Dim sb As New StringBuilder

            If _e1 IsNot Nothing Then
                sb.Append(_e1.First.ToString).Append(_e1.Second).Append(" - ")
            ElseIf _t1 IsNot Nothing Then
                sb.Append(_t1.First.RawName).Append(_t1.Second).Append(" - ")
                'Else
                '    sb.Append(_types.First.ToString).Append(_types.Second.ToString).Append(_key).Append(" - ")
            End If

            If _e2 IsNot Nothing Then
                sb.Append(_e2.First.ToString).Append(_e2.Second).Append(" - ")
            ElseIf _t2 IsNot Nothing Then
                sb.Append(_t2.First.RawName).Append(_t2.Second).Append(" - ")
                'Else
                '    sb.Append(_types.First.ToString).Append(_types.Second.ToString).Append(_key).Append(" - ")
            End If

            Return sb.ToString
        End Function

        Public Overrides Function ToString() As String
            Return _ToString()
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return _ToString.GetHashCode
        End Function

        Public Function ToStaticString(ByVal mpe As ObjectMappingEngine) As String Implements Core.IFilter.GetStaticString
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
        Implements IQueryElement
        Protected _table As SourceFragment
        Protected _joinType As Worm.Criteria.Joins.JoinType
        Protected _condition As Core.IFilter
        Protected _type As Type
        Protected _en As String
        Private _jt As Type
        Private _key As String

        Public Sub New(ByVal table As SourceFragment, ByVal joinType As Worm.Criteria.Joins.JoinType, ByVal condition As Core.IFilter)
            _table = table
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

        Public Sub New(ByVal type As Type, ByVal joinType As Worm.Criteria.Joins.JoinType, ByVal joinEntityType As Type)
            _type = type
            _joinType = joinType
            _condition = Condition
            _jt = joinEntityType
        End Sub

        Public Shared Function IsEmpty(ByVal j As OrmJoin) As Boolean
            Return j Is Nothing
        End Function

        'Public ReadOnly Property IsEmpty() As Boolean
        '    Get
        '        Return _table Is Nothing
        '    End Get
        'End Property

        Public Function JoinTypeString() As String
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

        Public Function GetStaticString(ByVal mpe As ObjectMappingEngine) As String Implements IQueryElement.GetStaticString
            If _table IsNot Nothing Then
                Return _table.RawName & JoinTypeString() & _condition.GetStaticString(mpe)
            ElseIf _type IsNot Nothing Then
                Return _type.ToString & JoinTypeString() & _condition.GetStaticString(mpe)
            Else
                Return _en & JoinTypeString() & _condition.GetStaticString(mpe)
            End If
        End Function

        Public Overrides Function ToString() As String Implements IQueryElement._ToString
            If _table IsNot Nothing Then
                Return _table.RawName & JoinTypeString() & _condition._ToString
            ElseIf _type IsNot Nothing Then
                Return _type.ToString & JoinTypeString() & _condition._ToString
            Else
                Return _en & JoinTypeString() & _condition._ToString
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
