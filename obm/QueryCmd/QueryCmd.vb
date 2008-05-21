Imports System.Collections.Generic
Imports Worm.Orm.Meta
Imports Worm.Criteria.Core
Imports Worm.Sorting
Imports Worm.Orm
Imports Worm.Criteria.Joins

Namespace Query

    Public Interface IExecutor
        Function Exec(Of ReturnType As {OrmBase, New})(ByVal mgr As OrmManagerBase, ByVal query As QueryCmdBase) As ReadOnlyList(Of ReturnType)
        Function Exec(Of SelectType As {OrmBase, New}, ReturnType)(ByVal mgr As OrmManagerBase, ByVal query As QueryCmdBase) As IList(Of ReturnType)
    End Interface

    Public Class Top
        Private _perc As Boolean
        Private _ties As Boolean
        Private _n As Integer

        Public Sub New(ByVal n As Integer)
            _n = n
        End Sub

        Public Sub New(ByVal n As Integer, ByVal percent As Boolean)
            MyClass.New(n)
            _perc = percent
        End Sub

        Public Sub New(ByVal n As Integer, ByVal percent As Boolean, ByVal ties As Boolean)
            MyClass.New(n, percent)
            _ties = ties
        End Sub

        Public ReadOnly Property Percent() As Boolean
            Get
                Return _perc
            End Get
        End Property

        Public ReadOnly Property Ties() As Boolean
            Get
                Return _ties
            End Get
        End Property

        Public ReadOnly Property Count() As Integer
            Get
                Return _n
            End Get
        End Property

        Public Function GetDynamicKey() As String
            Return "-top-" & _n.ToString & "-"
        End Function

        Public Function GetStaticKey() As String
            Return "-top-"
        End Function
    End Class

    Public Class QueryCmdBase
        Protected _fields As ObjectModel.ReadOnlyCollection(Of OrmProperty)
        Protected _filter As IGetFilter
        Protected _group As ObjectModel.ReadOnlyCollection(Of OrmProperty)
        Protected _order As Sort
        Protected _aggregates As ObjectModel.ReadOnlyCollection(Of AggregateBase)
        Protected _load As Boolean
        Protected _top As Top
        Protected _page As Nullable(Of Integer)
        Protected _distinct As Boolean
        Protected _dontcache As Boolean
        Protected _clientPage As Pair(Of Integer)
        Protected _joins() As OrmJoin
        Protected _autoJoins As Boolean
        Protected _table As SourceFragment
        Protected _hint As String
        Protected _mark As Integer = Environment.TickCount
        Protected _smark As Integer = Environment.TickCount
        Protected _t As Type
        Protected _o As OrmBase
        Protected _key As String

        Private _appendMain As Boolean

        Protected Friend ReadOnly Property Obj() As OrmBase
            Get
                Return _o
            End Get
        End Property

        Protected Friend ReadOnly Property Key() As String
            Get
                Return _key
            End Get
        End Property

        'Protected Friend Sub SetSelectList(ByVal l As ObjectModel.ReadOnlyCollection(Of OrmProperty))
        '    _fields = l
        'End Sub

        Private _exec As IExecutor

        Protected Function GetExecutor(ByVal mgr As OrmManagerBase) As IExecutor
            'If _dontcache Then
            If _exec Is Nothing Then
                _exec = mgr.ObjectSchema.CreateExecutor()
            End If
            Return _exec
            'Else
            'Return mgr.ObjectSchema.CreateExecutor()
            'End If
        End Function

        Protected ReadOnly Property AppendMain() As Boolean
            Get
                Return _appendMain
            End Get
        End Property

        Protected Sub OnSortChanged()
            _smark = Environment.TickCount
        End Sub

        Protected Sub New()
        End Sub

        Public Sub New(ByVal table As SourceFragment)
            _table = table
        End Sub

        Public Function Prepare(ByVal j As List(Of Worm.Criteria.Joins.OrmJoin), ByVal schema As QueryGenerator, ByVal filterInfo As Object, ByVal t As Type) As IFilter
            If Joins IsNot Nothing Then
                j.AddRange(Joins)
            End If

            Dim f As IFilter = Nothing
            If Filter IsNot Nothing Then
                f = Filter.Filter(t)
            End If

            If AutoJoins OrElse _o IsNot Nothing Then
                Dim joins() As Worm.Criteria.Joins.OrmJoin = Nothing
                If OrmManagerBase.HasJoins(schema, t, f, Sort, filterInfo, joins, _appendMain) Then
                    j.AddRange(joins)
                End If
            End If

            If _o IsNot Nothing Then
                Dim selectedType As Type = t
                Dim filteredType As Type = _o.GetType

                'Dim schema2 As IOrmObjectSchema = GetObjectSchema(filteredType)

                'column - select
                Dim selected_r As M2MRelation = Nothing
                'column - filter
                Dim filtered_r As M2MRelation = Nothing

                filtered_r = schema.GetM2MRelation(selectedType, filteredType, _key)
                selected_r = schema.GetM2MRelation(filteredType, selectedType, M2MRelation.GetRevKey(_key))

                If selected_r Is Nothing Then
                    Throw New QueryGeneratorException(String.Format("Type {0} has no relation to {1}", selectedType.Name, filteredType.Name))
                End If

                If filtered_r Is Nothing Then
                    Throw New QueryGeneratorException(String.Format("Type {0} has no relation to {1}", filteredType.Name, selectedType.Name))
                End If

                'Dim table As OrmTable = selected_r.Table
                Dim table As SourceFragment = filtered_r.Table

                If table Is Nothing Then
                    Throw New ArgumentException("Invalid relation", filteredType.ToString)
                End If

                'Dim table As OrmTable = _o.M2M.GetTable(t, _key)

                If _appendMain OrElse WithLoad Then
                    Dim jf As New Worm.Database.Criteria.Joins.JoinFilter(table, selected_r.Column, t, "ID", Criteria.FilterOperation.Equal)
                    Dim jn As New Worm.Database.Criteria.Joins.OrmJoin(table, JoinType.Join, jf)
                    j.Add(jn)
                    If table.Equals(_table) Then
                        _table = Nothing
                    End If
                    If WithLoad AndAlso _fields.Count = 1 Then
                        _fields = Nothing
                    End If
                Else
                    _table = table
                    Dim r As New List(Of OrmProperty)
                    'Dim os As IOrmObjectSchemaBase = schema.GetObjectSchema(selectedType)
                    'os.GetFieldColumnMap()("ID")._columnName
                    r.Add(New OrmProperty(table, selected_r.Column & " " & schema.GetColumnNameByFieldNameInternal(t, "ID", False), "ID"))
                    _fields = New ObjectModel.ReadOnlyCollection(Of OrmProperty)(r)
                End If

                Dim tf As New Worm.Database.Criteria.Core.TableFilter(table, filtered_r.Column, New Worm.Criteria.Values.ScalarValue(_o.Identifier), Criteria.FilterOperation.Equal)
                Dim con As Criteria.Conditions.Condition.ConditionConstructorBase = schema.CreateConditionCtor
                con.AddFilter(f)
                con.AddFilter(tf)
                f = con.Condition
            End If

            Return f
        End Function

        Public Function GetStaticKey(ByVal mgrKey As String, ByVal j As List(Of OrmJoin), ByVal f As IFilter) As String
            Dim key As String = String.Empty

            If f IsNot Nothing Then
                key &= f.ToStaticString & "$"
            End If

            If j IsNot Nothing Then
                For Each join As OrmJoin In j
                    If Not OrmJoin.IsEmpty(join) Then
                        key &= join.ToString
                    End If
                Next
            End If

            If _top IsNot Nothing Then
                key &= _top.GetStaticKey & "$"
            End If

            'key &= mgr.ObjectSchema.GetEntityKey(mgr.GetFilterInfo, GetType(T))

            Return key & "$" & mgrKey
        End Function

        Public Function GetDynamicKey(ByVal j As List(Of OrmJoin), ByVal f As IFilter) As String
            Dim id As String = String.Empty

            If f IsNot Nothing Then
                id &= f.ToString & "$"
            End If

            If j IsNot Nothing Then
                For Each join As OrmJoin In j
                    If Not OrmJoin.IsEmpty(join) Then
                        id &= join.ToString
                    End If
                Next
            End If

            If _top IsNot Nothing Then
                id &= _top.GetDynamicKey & "$"
            End If

            Return id '& GetType(T).ToString
        End Function

#Region " Properties "

        Public Overridable Property SelectedType() As Type
            Get
                Return _t
            End Get
            Set(ByVal value As Type)
                _t = value
            End Set
        End Property

        Public ReadOnly Property Mark() As Integer
            Get
                Return _mark
            End Get
        End Property

        Public ReadOnly Property SMark() As Integer
            Get
                Return _smark
            End Get
        End Property

        Public Property Hint() As String
            Get
                Return _hint
            End Get
            Set(ByVal value As String)
                _hint = value
                _smark = Environment.TickCount
            End Set
        End Property

        Public ReadOnly Property Table() As SourceFragment
            Get
                Return _table
            End Get
        End Property

        Public Property ClientPaging() As Pair(Of Integer)
            Get
                Return _clientPage
            End Get
            Set(ByVal value As Pair(Of Integer))
                _clientPage = value
            End Set
        End Property

        Public Property DontCache() As Boolean
            Get
                Return _dontcache
            End Get
            Set(ByVal value As Boolean)
                _dontcache = value
            End Set
        End Property

        Public Property AutoJoins() As Boolean
            Get
                Return _autoJoins
            End Get
            Set(ByVal value As Boolean)
                _autoJoins = value
                _mark = Environment.TickCount
            End Set
        End Property

        Public Property Group() As ObjectModel.ReadOnlyCollection(Of OrmProperty)
            Get
                Return _group
            End Get
            Set(ByVal value As ObjectModel.ReadOnlyCollection(Of OrmProperty))
                _group = value
                _mark = Environment.TickCount
            End Set
        End Property

        Public Property Joins() As OrmJoin()
            Get
                Return _joins
            End Get
            Set(ByVal value As OrmJoin())
                _joins = value
                _mark = Environment.TickCount
            End Set
        End Property

        Public Property Aggregates() As ObjectModel.ReadOnlyCollection(Of AggregateBase)
            Get
                Return _aggregates
            End Get
            Set(ByVal value As ObjectModel.ReadOnlyCollection(Of AggregateBase))
                _aggregates = value
                _mark = Environment.TickCount
            End Set
        End Property

        Public Property SelectList() As ObjectModel.ReadOnlyCollection(Of OrmProperty)
            Get
                Return _fields
            End Get
            Set(ByVal value As ObjectModel.ReadOnlyCollection(Of OrmProperty))
                _fields = value
                _mark = Environment.TickCount
            End Set
        End Property

        Public Property Sort() As Sort
            Get
                Return _order
            End Get
            Set(ByVal value As Sort)
                _order = value
                _mark = Environment.TickCount
                AddHandler value.OnChange, AddressOf OnSortChanged
            End Set
        End Property

        Public Property Top() As Top
            Get
                Return _top
            End Get
            Set(ByVal value As Top)
                _top = value
                _mark = Environment.TickCount
            End Set
        End Property

        Public Property Distinct() As Boolean
            Get
                Return _distinct
            End Get
            Set(ByVal value As Boolean)
                _distinct = value
                _mark = Environment.TickCount
            End Set
        End Property

        Public Property Filter() As IGetFilter
            Get
                Return _filter
            End Get
            Set(ByVal value As IGetFilter)
                _filter = value
                _mark = Environment.TickCount
            End Set
        End Property

        Public Property WithLoad() As Boolean
            Get
                Return _load
            End Get
            Set(ByVal value As Boolean)
                _load = value
                If _o Is Nothing Then
                    _smark = Environment.TickCount
                Else
                    _mark = Environment.TickCount
                End If
            End Set
        End Property
#End Region

    End Class

    Public Class QueryCmd(Of ReturnType As {OrmBase, New})
        Inherits QueryCmdBase

        Public Function Exec(ByVal mgr As OrmManagerBase) As ReadOnlyList(Of ReturnType)
            Return GetExecutor(mgr).Exec(Of ReturnType)(mgr, Me)
        End Function

        Public Function Exec(Of T)(ByVal mgr As OrmManagerBase) As IList(Of T)
            Return GetExecutor(mgr).Exec(Of ReturnType, T)(mgr, Me)
        End Function

        Public Overrides Property SelectedType() As System.Type
            Get
                Return GetType(ReturnType)
            End Get
            Set(ByVal value As System.Type)
                Throw New NotSupportedException
            End Set
        End Property

        Public Shared Function Create() As QueryCmd(Of ReturnType)
            Return Create(Nothing, Nothing, False)
        End Function

        Public Shared Function Create(ByVal filter As IGetFilter) As QueryCmd(Of ReturnType)
            Return Create(filter, Nothing, False)
        End Function

        Public Shared Function Create(ByVal table As SourceFragment, ByVal field As String) As QueryCmd(Of ReturnType)
            Dim q As QueryCmd(Of ReturnType) = Create(table, Nothing, Nothing, False)
            Dim l As New List(Of OrmProperty)
            l.Add(New OrmProperty(table, field))
            q._fields = New ObjectModel.ReadOnlyCollection(Of OrmProperty)(l)
            Return q
        End Function

        Public Shared Function Create(ByVal table As SourceFragment, ByVal fields() As String) As QueryCmd(Of ReturnType)
            Dim q As QueryCmd(Of ReturnType) = Create(table, Nothing, Nothing, False)
            Dim l As New List(Of OrmProperty)
            For Each f As String In fields
                l.Add(New OrmProperty(table, f))
            Next
            q._fields = New ObjectModel.ReadOnlyCollection(Of OrmProperty)(l)
            Return q
        End Function

        Public Shared Function Create(ByVal table As SourceFragment, ByVal fields() As OrmProperty) As QueryCmd(Of ReturnType)
            Dim q As QueryCmd(Of ReturnType) = Create(table, Nothing, Nothing, False)
            Dim l As New List(Of OrmProperty)
            l.AddRange(fields)
            q._fields = New ObjectModel.ReadOnlyCollection(Of OrmProperty)(l)
            Return q
        End Function

        Public Shared Function Create(ByVal filter As IGetFilter, ByVal sort As Sort) As QueryCmd(Of ReturnType)
            Return Create(filter, sort, False)
        End Function

        Public Shared Function Create(ByVal table As SourceFragment, ByVal filter As IGetFilter, ByVal sort As Sort) As QueryCmd(Of ReturnType)
            Return Create(table, filter, sort, False)
        End Function

        Public Shared Function Create(ByVal filter As IGetFilter, ByVal sort As Sort, ByVal withLoad As Boolean) As QueryCmd(Of ReturnType)
            Dim q As New QueryCmd(Of ReturnType)
            With q
                ._filter = filter
                ._order = sort
                ._load = withLoad
            End With
            If sort IsNot Nothing Then
                AddHandler sort.OnChange, AddressOf q.OnSortChanged
            End If
            Return q
        End Function

        Public Shared Function Create(ByVal table As SourceFragment, ByVal filter As IGetFilter, ByVal sort As Sort, ByVal withLoad As Boolean) As QueryCmd(Of ReturnType)
            Dim q As QueryCmd(Of ReturnType) = Create(filter, sort, withLoad)
            q._table = table
            Return q
        End Function

        Public Shared Function Create(ByVal o As OrmBase) As QueryCmd(Of ReturnType)
            Return Create(o, Nothing, Nothing, False, Nothing)
        End Function

        Public Shared Function Create(ByVal o As OrmBase, ByVal key As String) As QueryCmd(Of ReturnType)
            Return Create(o, Nothing, Nothing, False, key)
        End Function

        Public Shared Function Create(ByVal o As OrmBase, ByVal direct As Boolean) As QueryCmd(Of ReturnType)
            Dim key As String
            If direct Then
                key = M2MRelation.DirKey
            Else
                key = M2MRelation.RevKey
            End If

            Return Create(o, Nothing, Nothing, False, key)
        End Function

        Public Shared Function Create(ByVal o As OrmBase, ByVal filter As IGetFilter, ByVal sort As Sort, ByVal withLoad As Boolean, ByVal key As String) As QueryCmd(Of ReturnType)
            Dim q As QueryCmd(Of ReturnType) = Create(filter, sort, withLoad)
            With q
                ._o = o
                ._key = key
            End With

            Return q
        End Function

        Public Shared Function Create(ByVal aggregates() As AggregateBase) As QueryCmd(Of ReturnType)
            Dim q As QueryCmd(Of ReturnType) = Create(CType(Nothing, IFilter))
            q._aggregates = New ObjectModel.ReadOnlyCollection(Of AggregateBase)(aggregates)
            Return q
        End Function

#Region " Extensions "

        Public Function [Single](ByVal mgr As OrmManagerBase) As ReturnType
            Dim r As ReadOnlyList(Of ReturnType) = Exec(mgr)
            If r.Count <> 1 Then
                Throw New InvalidOperationException
            Else
                Return r(0)
            End If
        End Function

        Public Function ToList(ByVal mgr As OrmManagerBase) As ReadOnlyList(Of ReturnType)
            Return Exec(mgr)
        End Function

        Public Function ToSimpleList(Of T)(ByVal mgr As OrmManagerBase) As IList(Of T)
            Return Exec(Of T)(mgr)
        End Function
#End Region

    End Class

End Namespace