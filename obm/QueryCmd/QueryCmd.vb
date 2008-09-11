Imports System.Collections.Generic
Imports Worm.Orm.Meta
Imports Worm.Criteria.Core
Imports Worm.Sorting
Imports Worm.Orm
Imports Worm.Criteria.Joins
Imports System.Reflection

Namespace Query

    Public Interface IExecutor

        Function ExecEntity(Of ReturnType As {_IEntity})( _
            ByVal mgr As OrmManagerBase, ByVal query As QueryCmdBase) As ReadOnlyObjectList(Of ReturnType)

        Function Exec(Of ReturnType As _ICachedEntity)( _
            ByVal mgr As OrmManagerBase, ByVal query As QueryCmdBase) As ReadOnlyEntityList(Of ReturnType)

        Function Exec(Of SelectType As {_ICachedEntity, New}, ReturnType As _ICachedEntity)( _
            ByVal mgr As OrmManagerBase, ByVal query As QueryCmdBase) As ReadOnlyEntityList(Of ReturnType)

        Function ExecSimple(Of SelectType As {_ICachedEntity, New}, ReturnType)( _
            ByVal mgr As OrmManagerBase, ByVal query As QueryCmdBase) As IList(Of ReturnType)

        Function ExecSimple(Of ReturnType)( _
            ByVal mgr As OrmManagerBase, ByVal query As QueryCmdBase) As IList(Of ReturnType)

        Sub Reset(Of SelectType As {_ICachedEntity, New}, ReturnType As _ICachedEntity)(ByVal mgr As OrmManagerBase, ByVal query As QueryCmdBase)
        Sub Reset(Of ReturnType As _ICachedEntity)(ByVal mgr As OrmManagerBase, ByVal query As QueryCmdBase)
        Sub ResetEntity(Of ReturnType As _IEntity)(ByVal mgr As OrmManagerBase, ByVal query As QueryCmdBase)

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
        Implements ICloneable

        Public Delegate Function GetManagerDelegate() As OrmManagerBase

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
        'Protected _returnType As Type
        Protected _realType As Type
        Protected _o As _IOrmBase
        Protected _m2mKey As String
        Protected _rn As Worm.Database.Criteria.Core.TableFilter
        Protected _outer As QueryCmdBase
        Private _er As OrmManagerBase.ExecutionResult
        Private _en As String

        Private _appendMain As Boolean

        Private _createType As Type
        Public Property CreateType() As Type
            Get
                Return _createType
            End Get
            Set(ByVal value As Type)
                _createType = value
            End Set
        End Property

        Public Property LastExecitionResult() As OrmManagerBase.ExecutionResult
            Get
                Return _er
            End Get
            Protected Friend Set(ByVal value As OrmManagerBase.ExecutionResult)
                _er = value
            End Set
        End Property

        Protected Friend ReadOnly Property Obj() As _IOrmBase
            Get
                Return _o
            End Get
        End Property

        Protected Friend ReadOnly Property M2MKey() As String
            Get
                Return _m2mKey
            End Get
        End Property

        'Protected Friend Sub SetSelectList(ByVal l As ObjectModel.ReadOnlyCollection(Of OrmProperty))
        '    _fields = l
        'End Sub

        Private _exec As IExecutor

        Public Function GetExecutor(ByVal mgr As OrmManagerBase) As IExecutor
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

        Public Sub New()
        End Sub

        Public Sub New(ByVal table As SourceFragment)
            _table = table
        End Sub

        Public Sub New(ByVal selectType As Type)
            _realType = selectType
        End Sub

        Public Sub New(ByVal entityName As String)
            _en = entityName
        End Sub

        Public Sub New(ByVal obj As _IOrmBase)
            _o = obj
        End Sub

        Public Sub New(ByVal obj As _IOrmBase, ByVal key As String)
            _o = obj
            _m2mKey = key
        End Sub

        Public Function Prepare(ByVal js As List(Of List(Of Worm.Criteria.Joins.OrmJoin)), ByVal schema As QueryGenerator, ByVal filterInfo As Object, ByVal t As Type) As IFilter()
            Dim i As Integer = 0
            Dim q As QueryCmdBase = Me
            Dim fs As New List(Of IFilter)
            Do While q IsNot Nothing
                Dim j As New List(Of Worm.Criteria.Joins.OrmJoin)
                Dim f As IFilter = q.Prepare(j, schema, filterInfo, t)
                fs.Add(f)
                js.Add(j)
                q = q.OuterQuery
            Loop

            Return fs.ToArray
        End Function

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
                If OrmManagerBase.HasJoins(schema, t, f, propSort, filterInfo, joins, _appendMain) Then
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

                filtered_r = schema.GetM2MRelation(selectedType, filteredType, _m2mKey)
                selected_r = schema.GetM2MRelation(filteredType, selectedType, M2MRelation.GetRevKey(_m2mKey))

                If selected_r Is Nothing Then
                    Throw New QueryGeneratorException(String.Format("Type {0} has no relation to {1}", selectedType.Name, filteredType.Name))
                End If

                If filtered_r Is Nothing Then
                    Throw New QueryGeneratorException(String.Format("Type {0} has no relation to {1}", filteredType.Name, selectedType.Name))
                End If

                'Dim table As SourceFragment = selected_r.Table
                Dim table As SourceFragment = filtered_r.Table

                If table Is Nothing Then
                    Throw New ArgumentException("Invalid relation", filteredType.ToString)
                End If

                'Dim table As OrmTable = _o.M2M.GetTable(t, _key)

                If _appendMain OrElse propWithLoad Then
                    Dim jf As New Worm.Database.Criteria.Joins.JoinFilter(table, selected_r.Column, t, "ID", Criteria.FilterOperation.Equal)
                    Dim jn As New Worm.Database.Criteria.Joins.OrmJoin(table, JoinType.Join, jf)
                    j.Add(jn)
                    If table.Equals(_table) Then
                        _table = Nothing
                    End If
                    If propWithLoad AndAlso _fields.Count = 1 Then
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

        Public Function GetStaticKey(ByVal mgrKey As String, ByVal js As List(Of List(Of OrmJoin)), ByVal fs() As IFilter) As String
            Dim key As New StringBuilder
            Dim i As Integer = 0
            Dim q As QueryCmdBase = Me
            Do While q IsNot Nothing
                q.GetStaticKey(key, js(i), fs(i))
                i += 1
                q = q._outer
                If q IsNot Nothing Then
                    key.Append("$inner:")
                End If
            Loop

            'key &= mgr.ObjectSchema.GetEntityKey(mgr.GetFilterInfo, GetType(T))

            key.Append("$").Append(mgrKey)
            Return key.ToString
        End Function

        Protected Friend Sub GetStaticKey(ByVal sb As StringBuilder, ByVal j As List(Of OrmJoin), ByVal f As IFilter)
            If f IsNot Nothing Then
                sb.Append(f.ToStaticString).Append("$")
            End If

            If _rn IsNot Nothing Then
                sb.Append(_rn.ToStaticString)
            End If

            If j IsNot Nothing Then
                For Each join As OrmJoin In j
                    If Not OrmJoin.IsEmpty(join) Then
                        sb.Append(join.ToString)
                    End If
                Next
            End If

            If _top IsNot Nothing Then
                sb.Append(_top.GetStaticKey).Append("$")
            End If
        End Sub

        Public Function GetDynamicKey(ByVal js As List(Of List(Of OrmJoin)), ByVal fs() As IFilter) As String
            Dim id As New StringBuilder

            Dim i As Integer = 0
            Dim q As QueryCmdBase = Me
            Do While q IsNot Nothing
                q.GetDynamicKey(id, js(i), fs(i))
                i += 1
                q = q._outer
                If q IsNot Nothing Then
                    id.Append("$inner:")
                End If
            Loop

            Return id.ToString '& GetType(T).ToString
        End Function

        Protected Friend Sub GetDynamicKey(ByVal sb As StringBuilder, ByVal j As List(Of OrmJoin), ByVal f As IFilter)
            If f IsNot Nothing Then
                sb.Append(f.ToString).Append("$")
            End If

            If j IsNot Nothing Then
                For Each join As OrmJoin In j
                    If Not OrmJoin.IsEmpty(join) Then
                        sb.Append(join.ToString)
                    End If
                Next
            End If

            If _top IsNot Nothing Then
                sb.Append(_top.GetDynamicKey).Append("$")
            End If

            Dim cnt As Integer = 0
            Dim n As Sort = _order
            Do While n IsNot Nothing
                cnt += 1
                n = n.Previous
            Loop
            If cnt > 0 Then
                sb.Append("sort=").Append(cnt).Append("$")
            End If

            If _rn IsNot Nothing Then
                sb.Append(_rn.ToString)
            End If
        End Sub

#Region " Properties "
        Public Property EntityName() As String
            Get
                Return _en
            End Get
            Set(ByVal value As String)
                _en = value
            End Set
        End Property

        Public Property OuterQuery() As QueryCmdBase
            Get
                Return _outer
            End Get
            Set(ByVal value As QueryCmdBase)
                _outer = value
                _mark = Environment.TickCount
            End Set
        End Property

        Public Const RowNumerColumn As String = "[worm.row_number]"

        Public Property RowNumberFilter() As Worm.Database.Criteria.Core.TableFilter
            Get
                Return _rn
            End Get
            Set(ByVal value As Worm.Database.Criteria.Core.TableFilter)
                _rn = value
                _mark = Environment.TickCount
            End Set
        End Property

        Public Overridable Property SelectedType() As Type
            Get
                Return _realType
            End Get
            Set(ByVal value As Type)
                _realType = value
            End Set
        End Property

        Public Overridable ReadOnly Property ReturnType() As Type
            Get
                Return _realType
            End Get
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

        Public Property propSort() As Sort
            Get
                Return _order
            End Get
            Set(ByVal value As Sort)
                _order = value
                _mark = Environment.TickCount
                AddHandler value.OnChange, AddressOf OnSortChanged
            End Set
        End Property

        Public Property propTop() As Top
            Get
                Return _top
            End Get
            Set(ByVal value As Top)
                _top = value
                _mark = Environment.TickCount
            End Set
        End Property

        Public Property propDistinct() As Boolean
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

        Public Property propWithLoad() As Boolean
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

        Public Function Distinct(ByVal value As Boolean) As QueryCmdBase
            propDistinct = value
            Return Me
        End Function

        Public Function Top(ByVal value As Integer) As QueryCmdBase
            propTop = New Query.Top(value)
            Return Me
        End Function

        Public Function Sort(ByVal value As Sort) As QueryCmdBase
            propSort = value
            Return Me
        End Function

        Public Function WithLoad(ByVal value As Boolean) As QueryCmdBase
            propWithLoad = value
            Return Me
        End Function

        Public Function Where(ByVal filter As IGetFilter) As QueryCmdBase
            Me.Filter = filter
            Return Me
        End Function

        Public Function From(ByVal t As SourceFragment) As QueryCmdBase
            _table = t
            _mark = Environment.TickCount
            Return Me
        End Function

        Public Function [Select](ByVal fields() As OrmProperty) As QueryCmdBase
            SelectList = New ObjectModel.ReadOnlyCollection(Of OrmProperty)(fields)
            Return Me
        End Function

        Public Function GroupBy(ByVal fields() As OrmProperty) As QueryCmdBase
            Group = New ObjectModel.ReadOnlyCollection(Of OrmProperty)(fields)
            Return Me
        End Function

        'Protected Function CreateTypedCmd(ByVal qt As Type) As QueryCmdBase
        '    Dim qgt As Type = GetType(QueryCmd(Of ))
        '    Dim t As Type = qgt.MakeGenericType(New Type() {qt})
        '    Return CType(Activator.CreateInstance(t), QueryCmdBase)
        'End Function

        'Protected Function CreateTypedCopy(ByVal qt As Type) As QueryCmdBase
        '    Dim q As QueryCmdBase = CreateTypedCmd(qt)
        '    CopyTo(q)
        '    Return q
        'End Function

        'Public Function ToListTypeless(ByVal mgr As OrmManagerBase) As IList
        '    Dim q As QueryCmdBase = CreateTypedCopy(SelectedType)
        '    Dim e As IList = CType(q.GetType.InvokeMember("_Exec", Reflection.BindingFlags.Instance Or Reflection.BindingFlags.NonPublic Or Reflection.BindingFlags.InvokeMethod, _
        '                           Nothing, q, New Object() {mgr}), System.Collections.IList)
        '    Return e
        'End Function

        Private Class cls
            Private _m As GetManagerDelegate

            Public Sub New(ByVal getMgr As GetManagerDelegate)
                _m = getMgr
            End Sub

            Protected Sub GetManager(ByVal o As IEntity, ByVal args As ManagerRequiredArgs)
                args.Manager = _m()
            End Sub

            Public Sub ObjectCreated(ByVal o As ICachedEntity, ByVal mgr As OrmManagerBase)
                AddHandler o.ManagerRequired, AddressOf GetManager
            End Sub
        End Class

        Public Function ToList(ByVal mgr As OrmManagerBase) As IList
            Dim t As MethodInfo = Me.GetType.GetMethod("ToEntityList", New Type() {GetType(OrmManagerBase)})
            t = t.MakeGenericMethod(New Type() {SelectedType})
            Return CType(t.Invoke(Me, New Object() {mgr}), System.Collections.IList)
        End Function

        Public Function ToEntityList(Of T As {_ICachedEntity})(ByVal getMgr As GetManagerDelegate) As ReadOnlyEntityList(Of T)
            Dim mgr As OrmManagerBase = getMgr()
            Try
                mgr.RaiseObjectCreation = True
                AddHandler mgr.ObjectCreated, AddressOf New cls(getMgr).ObjectCreated
                'AddHandler mgr.ObjectCreated, Function(o As ICachedEntity, m As OrmManagerBase) AddHandler o.ManagerRequired,function(ByVal o As IEntity, ByVal args As ManagerRequiredArgs) args.Manager = getmgr)
                Return ToEntityList(Of T)(mgr)
            Finally
                If mgr IsNot Nothing Then
                    mgr.Dispose()
                End If
            End Try
        End Function

        Public Function ToObjectList(Of T As _IEntity)(ByVal mgr As OrmManagerBase) As ReadOnlyObjectList(Of T)
            Return GetExecutor(mgr).ExecEntity(Of T)(mgr, Me)
        End Function

        Public Function ToEntityList(Of T As {_ICachedEntity})(ByVal mgr As OrmManagerBase) As ReadOnlyEntityList(Of T)
            Return GetExecutor(mgr).Exec(Of T)(mgr, Me)
        End Function

        Public Function ToOrmList(Of T As {_IOrmBase})(ByVal mgr As OrmManagerBase) As ReadOnlyList(Of T)
            Return CType(ToEntityList(Of T)(mgr), ReadOnlyList(Of T))
        End Function

        Public Function ToList(Of T As {_ICachedEntity})(ByVal mgr As OrmManagerBase) As IList(Of T)
            Return ToEntityList(Of T)(mgr)
        End Function

        Public Function ToList(Of SelectType As {_ICachedEntity, New}, ReturnType As {_ICachedEntity})(ByVal mgr As OrmManagerBase) As IList(Of ReturnType)
            Return GetExecutor(mgr).Exec(Of SelectType, ReturnType)(mgr, Me)
        End Function

        'Public Function ExecTypeless(ByVal mgr As OrmManagerBase) As IEnumerator
        '    Return ToListTypeless(mgr).GetEnumerator
        'End Function

        Public Function ToSimpleList(Of T)(ByVal mgr As OrmManagerBase) As IList(Of T)
            Return GetExecutor(mgr).ExecSimple(Of T)(mgr, Me)
            'Dim q As QueryCmdBase = CreateTypedCopy(SelectedType)
            'Dim qt As Type = q.GetType
            'Dim mi As MethodInfo = qt.GetMethod("ToSimpleList")
            'If mi Is Nothing Then
            '    Throw New InvalidOperationException
            'End If
            'Dim rmi As MethodInfo = mi.MakeGenericMethod(New Type() {GetType(T)})
            'Return CType(rmi.Invoke(q, New Object() {mgr}), Global.System.Collections.Generic.IList(Of T))
        End Function

        Public Sub Reset(Of ReturnType As _IEntity)(ByVal mgr As OrmManagerBase)
            GetExecutor(mgr).ResetEntity(Of ReturnType)(mgr, Me)
        End Sub

        Public Sub Renew(Of ReturnType As _ICachedEntity)(ByVal mgr As OrmManagerBase)
            GetExecutor(mgr).Reset(Of ReturnType)(mgr, Me)
        End Sub

        Public Sub Renew(Of SelectType As {_ICachedEntity, New}, ReturnType As _ICachedEntity)(ByVal mgr As OrmManagerBase)
            GetExecutor(mgr).Reset(Of SelectType, ReturnType)(mgr, Me)
        End Sub

        Public Sub CopyTo(ByVal o As QueryCmdBase)
            With o
                ._aggregates = _aggregates
                ._appendMain = _appendMain
                ._autoJoins = _autoJoins
                ._clientPage = _clientPage
                ._distinct = _distinct
                ._dontcache = _dontcache
                ._exec = _exec
                ._fields = _fields
                ._filter = _filter
                ._group = _group
                ._hint = _hint
                ._joins = _joins
                ._m2mKey = _m2mKey
                ._load = _load
                ._mark = ._mark
                ._o = _o
                ._order = _order
                ._page = _page
                ._smark = _smark
                ._realType = _realType
                ._table = _table
                ._top = _top
                ._rn = _rn
                ._outer = _outer
            End With
        End Sub

        Public Function Clone() As Object Implements System.ICloneable.Clone
            Dim q As New QueryCmdBase
            CopyTo(q)
            Return q
        End Function

        '#Region " Factory "

        '        Public Shared Function Create(Of ReturnType As {_ICachedEntity, New})() As QueryCmd(Of ReturnType)
        '            Return Create(Of ReturnType)(Nothing, Nothing, False)
        '        End Function

        '        Public Shared Function Create(Of ReturnType As {_ICachedEntity, New})(ByVal filter As IGetFilter) As QueryCmd(Of ReturnType)
        '            Return Create(Of ReturnType)(filter, Nothing, False)
        '        End Function

        '        Public Shared Function Create(Of ReturnType As {_ICachedEntity, New})(ByVal table As SourceFragment, ByVal column As String, ByVal field As String) As QueryCmd(Of ReturnType)
        '            Dim q As QueryCmd(Of ReturnType) = Create(Of ReturnType)(table, Nothing, Nothing, False)
        '            Dim l As New List(Of OrmProperty)
        '            l.Add(New OrmProperty(table, column, field))
        '            q._fields = New ObjectModel.ReadOnlyCollection(Of OrmProperty)(l)
        '            Return q
        '        End Function

        '        Public Shared Function Create(Of ReturnType As {_ICachedEntity, New})(ByVal table As SourceFragment, ByVal columnAndField() As Pair(Of String, String)) As QueryCmd(Of ReturnType)
        '            Dim q As QueryCmd(Of ReturnType) = Create(Of ReturnType)(table, Nothing, Nothing, False)
        '            Dim l As New List(Of OrmProperty)
        '            For Each f As Pair(Of String, String) In columnAndField
        '                l.Add(New OrmProperty(table, f.First, f.Second))
        '            Next
        '            q._fields = New ObjectModel.ReadOnlyCollection(Of OrmProperty)(l)
        '            Return q
        '        End Function

        '        Public Shared Function Create(Of ReturnType As {_ICachedEntity, New})(ByVal table As SourceFragment, ByVal fields() As OrmProperty) As QueryCmd(Of ReturnType)
        '            Dim q As QueryCmd(Of ReturnType) = Create(Of ReturnType)(table, Nothing, Nothing, False)
        '            Dim l As New List(Of OrmProperty)
        '            l.AddRange(fields)
        '            q._fields = New ObjectModel.ReadOnlyCollection(Of OrmProperty)(l)
        '            Return q
        '        End Function

        '        Public Shared Function Create(Of ReturnType As {_ICachedEntity, New})(ByVal filter As IGetFilter, ByVal sort As Sort) As QueryCmd(Of ReturnType)
        '            Return Create(Of ReturnType)(filter, sort, False)
        '        End Function

        '        Public Shared Function Create(Of ReturnType As {_ICachedEntity, New})(ByVal table As SourceFragment, ByVal filter As IGetFilter, ByVal sort As Sort) As QueryCmd(Of ReturnType)
        '            Return Create(Of ReturnType)(table, filter, sort, False)
        '        End Function

        '        Public Shared Function Create(Of ReturnType As {_ICachedEntity, New})(ByVal filter As IGetFilter, ByVal sort As Sort, ByVal withLoad As Boolean) As QueryCmd(Of ReturnType)
        '            Dim q As New QueryCmd(Of ReturnType)
        '            With q
        '                ._filter = filter
        '                ._order = sort
        '                ._load = withLoad
        '            End With
        '            If sort IsNot Nothing Then
        '                AddHandler sort.OnChange, AddressOf q.OnSortChanged
        '            End If
        '            Return q
        '        End Function

        '        Public Shared Function Create(Of ReturnType As {_ICachedEntity, New})(ByVal table As SourceFragment, ByVal filter As IGetFilter, ByVal sort As Sort, ByVal withLoad As Boolean) As QueryCmd(Of ReturnType)
        '            Dim q As QueryCmd(Of ReturnType) = Create(Of ReturnType)(filter, sort, withLoad)
        '            q._table = table
        '            Return q
        '        End Function

        '        Public Shared Function Create(Of ReturnType As {_ICachedEntity, New})(ByVal o As OrmBase) As QueryCmd(Of ReturnType)
        '            Return Create(Of ReturnType)(o, Nothing, Nothing, False, Nothing)
        '        End Function

        '        Public Shared Function Create(Of ReturnType As {_ICachedEntity, New})(ByVal o As OrmBase, ByVal key As String) As QueryCmd(Of ReturnType)
        '            Return Create(Of ReturnType)(o, Nothing, Nothing, False, key)
        '        End Function

        '        Public Shared Function Create(Of ReturnType As {_ICachedEntity, New})(ByVal o As OrmBase, ByVal direct As Boolean) As QueryCmd(Of ReturnType)
        '            Dim key As String
        '            If direct Then
        '                key = M2MRelation.DirKey
        '            Else
        '                key = M2MRelation.RevKey
        '            End If

        '            Return Create(Of ReturnType)(o, Nothing, Nothing, False, key)
        '        End Function

        '        Public Shared Function Create(Of ReturnType As {_ICachedEntity, New})(ByVal o As OrmBase, ByVal filter As IGetFilter, ByVal sort As Sort, ByVal withLoad As Boolean, ByVal key As String) As QueryCmd(Of ReturnType)
        '            Dim q As QueryCmd(Of ReturnType) = Create(Of ReturnType)(filter, sort, withLoad)
        '            With q
        '                ._o = o
        '                ._m2mKey = key
        '            End With

        '            Return q
        '        End Function

        '        Public Shared Function Create(Of ReturnType As {_ICachedEntity, New})(ByVal aggregates() As AggregateBase) As QueryCmd(Of ReturnType)
        '            Dim q As QueryCmd(Of ReturnType) = Create(Of ReturnType)(CType(Nothing, IFilter))
        '            q._aggregates = New ObjectModel.ReadOnlyCollection(Of AggregateBase)(aggregates)
        '            Return q
        '        End Function

        '#End Region

    End Class

    'Public Class QueryCmd(Of ReturnType As {_ICachedEntity, New})
    '    Inherits QueryCmdBase

    '    Public Overrides Property SelectedType() As System.Type
    '        Get
    '            Return GetType(ReturnType)
    '        End Get
    '        Set(ByVal value As System.Type)
    '            Throw New NotSupportedException
    '        End Set
    '    End Property

    '    Public Function Exec(ByVal mgr As OrmManagerBase) As ReadOnlyEntityList(Of ReturnType)
    '        Return GetExecutor(mgr).Exec(Of ReturnType)(mgr, Me)
    '    End Function

    '    Public Function [Single](ByVal mgr As OrmManagerBase) As ReturnType
    '        Dim r As ReadOnlyEntityList(Of ReturnType) = Exec(mgr)
    '        If r.Count <> 1 Then
    '            Throw New InvalidOperationException
    '        Else
    '            Return r(0)
    '        End If
    '    End Function

    '    Public Function Exec(Of T)(ByVal mgr As OrmManagerBase) As IList(Of T)
    '        Return GetExecutor(mgr).ExecSimple(Of ReturnType, T)(mgr, Me)
    '    End Function
    'End Class

    'Public Class QueryEntity(Of ReturnType As {_IEntity, New})
    '    Inherits QueryCmdBase

    '    'Protected Function _Exec(ByVal mgr As OrmManagerBase) As ReadOnlyList(Of ReturnType)
    '    '    Return GetExecutor(mgr).Exec(Of ReturnType)(mgr, Me)
    '    'End Function

    '    Public Function Exec(ByVal mgr As OrmManagerBase) As ReadOnlyObjectList(Of ReturnType)
    '        Return GetExecutor(mgr).ExecEntity(Of ReturnType)(mgr, Me)
    '    End Function

    '    'Public Function Exec(Of T)(ByVal mgr As OrmManagerBase) As IList(Of T)
    '    '    Return GetExecutor(mgr).Exec(Of ReturnType, T)(mgr, Me)
    '    'End Function

    '    Public Overrides Property SelectedType() As System.Type
    '        Get
    '            Return GetType(ReturnType)
    '        End Get
    '        Set(ByVal value As System.Type)
    '            Throw New NotSupportedException
    '        End Set
    '    End Property

    '    Public Function [Single](ByVal mgr As OrmManagerBase) As ReturnType
    '        Dim r As ReadOnlyObjectList(Of ReturnType) = Exec(mgr)
    '        If r.Count <> 1 Then
    '            Throw New InvalidOperationException
    '        Else
    '            Return r(0)
    '        End If
    '    End Function

    'End Class

End Namespace