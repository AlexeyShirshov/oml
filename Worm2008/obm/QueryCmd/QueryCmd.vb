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
            ByVal mgr As OrmManager, ByVal query As QueryCmd) As ReadOnlyObjectList(Of ReturnType)

        Function Exec(Of ReturnType As _ICachedEntity)( _
            ByVal mgr As OrmManager, ByVal query As QueryCmd) As ReadOnlyEntityList(Of ReturnType)

        Function Exec(Of SelectType As {_ICachedEntity, New}, ReturnType As _ICachedEntity)( _
            ByVal mgr As OrmManager, ByVal query As QueryCmd) As ReadOnlyEntityList(Of ReturnType)

        Function ExecSimple(Of SelectType As {_ICachedEntity, New}, ReturnType)( _
            ByVal mgr As OrmManager, ByVal query As QueryCmd) As IList(Of ReturnType)

        Function ExecSimple(Of ReturnType)( _
            ByVal mgr As OrmManager, ByVal query As QueryCmd) As IList(Of ReturnType)

        Sub Reset(Of SelectType As {_ICachedEntity, New}, ReturnType As _ICachedEntity)(ByVal mgr As OrmManager, ByVal query As QueryCmd)
        Sub Reset(Of ReturnType As _ICachedEntity)(ByVal mgr As OrmManager, ByVal query As QueryCmd)
        Sub ResetEntity(Of ReturnType As _IEntity)(ByVal mgr As OrmManager, ByVal query As QueryCmd)

    End Interface

    Public Interface ICreateQueryCmd
        Function Create(ByVal table As SourceFragment) As QueryCmd

        Function Create(ByVal selectType As Type) As QueryCmd

        Function CreateByEntityName(ByVal entityName As String) As QueryCmd

        Function Create(ByVal obj As _IOrmBase) As QueryCmd

        Function Create(ByVal obj As _IOrmBase, ByVal key As String) As QueryCmd

        Function Create(ByVal name As String, ByVal table As SourceFragment) As QueryCmd

        Function Create(ByVal name As String, ByVal selectType As Type) As QueryCmd

        Function CreateByEntityName(ByVal name As String, ByVal entityName As String) As QueryCmd

        Function Create(ByVal name As String, ByVal obj As _IOrmBase) As QueryCmd

        Function Create(ByVal name As String, ByVal obj As _IOrmBase, ByVal key As String) As QueryCmd
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

    Public Class Paging
        Public Start As Integer
        Public Length As Integer

        Public Sub New()
        End Sub

        Public Sub New(ByVal start As Integer, ByVal length As Integer)
            Me.Start = start
            Me.Length = length
        End Sub
    End Class

    Public Class QueryIterator
        Implements IEnumerator(Of QueryCmd), IEnumerable(Of QueryCmd)

        Private _q As QueryCmd
        Private _c As QueryCmd

        Public Sub New(ByVal query As QueryCmd)
            _q = query
        End Sub

        Public ReadOnly Property Current() As QueryCmd Implements System.Collections.Generic.IEnumerator(Of QueryCmd).Current
            Get
                Return _c
            End Get
        End Property

        Private ReadOnly Property _Current() As Object Implements System.Collections.IEnumerator.Current
            Get
                Return Current
            End Get
        End Property

        Public Function MoveNext() As Boolean Implements System.Collections.IEnumerator.MoveNext
            If _c Is Nothing Then
                _c = _q
            Else
                _c = _c.OuterQuery
            End If
            Return _c IsNot Nothing
        End Function

        Public Sub Reset() Implements System.Collections.IEnumerator.Reset
            _c = Nothing
        End Sub

#Region " IDisposable Support "
        Private disposedValue As Boolean = False        ' To detect redundant calls

        ' IDisposable
        Protected Overridable Sub Dispose(ByVal disposing As Boolean)
            If Not Me.disposedValue Then
                If disposing Then
                    ' TODO: free other state (managed objects).
                End If

                ' TODO: free your own state (unmanaged objects).
                ' TODO: set large fields to null.
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

        Public Function GetEnumerator() As System.Collections.Generic.IEnumerator(Of QueryCmd) Implements System.Collections.Generic.IEnumerable(Of QueryCmd).GetEnumerator
            Return Me
        End Function

        Private Function _GetEnumerator() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
            Return GetEnumerator()
        End Function
    End Class

    Public Class QueryCmd
        Implements ICloneable, Cache.IQueryDependentTypes, Criteria.Values.IQueryElement

        Protected _fields As ObjectModel.ReadOnlyCollection(Of SelectExpression)
        Protected _filter As IGetFilter
        Protected _group As ObjectModel.ReadOnlyCollection(Of Grouping)
        Protected _order As Sort
        Protected _aggregates As ObjectModel.ReadOnlyCollection(Of AggregateBase)
        Protected _load As Boolean
        Protected _top As Top
        Protected _page As Nullable(Of Integer)
        Protected _distinct As Boolean
        Protected _dontcache As Boolean
        Private _liveTime As TimeSpan
        Private _mgrMark As String
        Protected _clientPage As Paging
        Protected _joins() As OrmJoin
        Protected _autoJoins As Boolean
        Protected _table As SourceFragment
        Protected _hint As String
        Protected _mark As Guid = Guid.NewGuid 'Environment.TickCount
        Protected _statementMark As Guid = Guid.NewGuid 'Environment.TickCount
        'Protected _returnType As Type
        Protected _realType As Type
        Protected _o As _IOrmBase
        Protected _m2mKey As String
        Protected _rn As Worm.Database.Criteria.Core.TableFilter
        Protected _outer As QueryCmd
        Private _er As OrmManager.ExecutionResult
        Private _en As String
        Friend _resDic As Boolean
        Private _appendMain As Boolean
        Protected _getMgr As ICreateManager
        Private _name As String
        Private _execCnt As Integer
        Private _schema As ObjectMappingEngine
        Private _cacheSort As Boolean
        Private _autoFields As Boolean
        Private _timeout As Nullable(Of Integer)

        Private _createType As Type
        Public Property CreateType() As Type
            Get
                Return _createType
            End Get
            Set(ByVal value As Type)
                _createType = value
            End Set
        End Property

        Public Property AutoFields() As Boolean
            Get
                Return _autoFields
            End Get
            Set(ByVal value As Boolean)
                _autoFields = value
            End Set
        End Property

        Public Property CommandTimed() As Nullable(Of Integer)
            Get
                Return _timeout
            End Get
            Set(ByVal value As Nullable(Of Integer))
                _timeout = value
            End Set
        End Property

        Public Property Schema() As ObjectMappingEngine
            Get
                Return _schema
            End Get
            Set(ByVal value As ObjectMappingEngine)
                _schema = value
                RenewMark()
            End Set
        End Property

        Public Property ExecCount() As Integer
            Get
                Return _execCnt
            End Get
            Friend Set(ByVal value As Integer)
                _execCnt = value
            End Set
        End Property

        Public Property Name() As String
            Get
                Return _name
            End Get
            Set(ByVal value As String)
                _name = value
            End Set
        End Property

        Public Property LastExecitionResult() As OrmManager.ExecutionResult
            Get
                Return _er
            End Get
            Protected Friend Set(ByVal value As OrmManager.ExecutionResult)
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

        Public ReadOnly Property GetMgr() As ICreateManager
            Get
                Return _getMgr
            End Get
        End Property

        'Protected Friend Sub SetSelectList(ByVal l As ObjectModel.ReadOnlyCollection(Of OrmProperty))
        '    _fields = l
        'End Sub

        Private _exec As IExecutor

        Public Function GetExecutor(ByVal mgr As OrmManager) As IExecutor
            'If _dontcache Then
            If _exec Is Nothing Then
                _exec = mgr.MappingEngine.CreateExecutor()
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
            RenewStatementMark()
        End Sub

#Region " Ctors "
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

        Public Sub New(ByVal table As SourceFragment, ByVal getMgr As ICreateManager)
            _table = table
            _getMgr = getMgr
        End Sub

        Public Sub New(ByVal selectType As Type, ByVal getMgr As ICreateManager)
            _realType = selectType
            _getMgr = getMgr
        End Sub

        Public Sub New(ByVal entityName As String, ByVal getMgr As ICreateManager)
            _en = entityName
            _getMgr = getMgr
        End Sub

        Public Sub New(ByVal obj As _IOrmBase, ByVal getMgr As ICreateManager)
            _o = obj
            _getMgr = getMgr
        End Sub

        Public Sub New(ByVal obj As _IOrmBase, ByVal key As String, ByVal getMgr As ICreateManager)
            _o = obj
            _m2mKey = key
            _getMgr = getMgr
        End Sub

#End Region

        Protected Sub RenewMark()
            _mark = Guid.NewGuid 'Environment.TickCount
        End Sub

        Protected Sub RenewStatementMark()
            _statementMark = Guid.NewGuid 'Environment.TickCount
        End Sub

        Public Function Prepare(ByVal js As List(Of List(Of Worm.Criteria.Joins.OrmJoin)), _
            ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, ByVal t As Type, _
            ByVal cs As List(Of List(Of SelectExpression))) As IFilter()

            Dim fs As New List(Of IFilter)
            For Each q As QueryCmd In New QueryIterator(Me)
                Dim j As New List(Of Worm.Criteria.Joins.OrmJoin)
                Dim c As List(Of SelectExpression) = Nothing
                Dim f As IFilter = q.Prepare(j, schema, filterInfo, t, c)
                If f IsNot Nothing Then
                    fs.Add(f)
                End If

                js.Add(j)
                cs.Add(c)
            Next

            Return fs.ToArray
        End Function

        Public Function Prepare(ByVal j As List(Of Worm.Criteria.Joins.OrmJoin), _
            ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, ByVal t As Type, _
            ByRef cl As List(Of SelectExpression)) As IFilter

            If Joins IsNot Nothing Then
                j.AddRange(Joins)
            End If

            Dim f As IFilter = Nothing
            If Filter IsNot Nothing Then
                f = Filter.Filter(t)
            End If

            For Each s As Sort In New Sort.Iterator(_order)
                If s.Type Is Nothing AndAlso s.Table Is Nothing Then
                    s.Type = t
                End If

                If s.Type IsNot Nothing AndAlso s.Field Is Nothing AndAlso s.Column IsNot Nothing Then
                    s.Field = s.Column
                    s.Column = Nothing
                End If

            Next

            If AutoJoins OrElse _o IsNot Nothing Then
                Dim joins() As Worm.Criteria.Joins.OrmJoin = Nothing
                If OrmManager.HasJoins(schema, t, f, propSort, filterInfo, joins, _appendMain) Then
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
                    Throw New ObjectMappingException(String.Format("Type {0} has no relation to {1}", selectedType.Name, filteredType.Name))
                End If

                If filtered_r Is Nothing Then
                    Throw New ObjectMappingException(String.Format("Type {0} has no relation to {1}", filteredType.Name, selectedType.Name))
                End If

                'Dim table As SourceFragment = selected_r.Table
                Dim table As SourceFragment = filtered_r.Table

                If table Is Nothing Then
                    Throw New ArgumentException("Invalid relation", filteredType.ToString)
                End If

                'Dim table As OrmTable = _o.M2M.GetTable(t, _key)

                If _appendMain OrElse propWithLoad Then
                    Dim jf As New Worm.Database.Criteria.Joins.JoinFilter(table, selected_r.Column, t, OrmBaseT.PKName, Criteria.FilterOperation.Equal)
                    Dim jn As New Worm.Database.Criteria.Joins.OrmJoin(table, JoinType.Join, jf)
                    j.Add(jn)
                    If table.Equals(_table) Then
                        _table = Nothing
                    End If
                    If propWithLoad AndAlso _fields IsNot Nothing AndAlso _fields.Count = 1 Then
                        _fields = Nothing
                    End If
                Else
                    _table = table
                    Dim r As New List(Of SelectExpression)
                    'Dim os As IOrmObjectSchemaBase = schema.GetObjectSchema(selectedType)
                    'os.GetFieldColumnMap()("ID")._columnName
                    r.Add(New SelectExpression(table, selected_r.Column & " " & schema.GetColumnNameByFieldNameInternal(t, OrmBaseT.PKName, False), OrmBaseT.PKName))
                    r(0).Attributes = Field2DbRelations.PK
                    '_fields = New ObjectModel.ReadOnlyCollection(Of OrmProperty)(r)
                    cl = r
                End If

                Dim tf As New Worm.Database.Criteria.Core.TableFilter(table, filtered_r.Column, _
                    New Worm.Criteria.Values.ScalarValue(_o.Identifier), Criteria.FilterOperation.Equal)
                Dim con As Criteria.Conditions.Condition.ConditionConstructorBase = schema.CreateConditionCtor
                con.AddFilter(f)
                con.AddFilter(tf)
                f = con.Condition
            End If

            If _fields IsNot Nothing Then
                If t IsNot Nothing Then
                    If _autoFields Then
                        For Each pk As ColumnAttribute In schema.GetPrimaryKeys(t)
                            Dim find As Boolean
                            For Each fld As SelectExpression In _fields
                                If (fld.Attributes And Field2DbRelations.PK) = Field2DbRelations.PK _
                                    AndAlso fld.Field = pk.FieldName Then
                                    find = True
                                    Exit For
                                End If
                            Next
                            If Not find Then
                                If cl Is Nothing Then
                                    cl = New List(Of SelectExpression)
                                End If
                                cl.Add(New SelectExpression(t, pk.FieldName))
                                cl(0).Attributes = pk._behavior
                            End If
                        Next
                    ElseIf cl Is Nothing Then
                        cl = New List(Of SelectExpression)
                    End If

                    If cl IsNot Nothing Then
                        For Each fld As SelectExpression In _fields
                            If Not cl.Contains(fld) Then
                                cl.Add(fld)
                            End If
                        Next
                    End If
                Else
                    cl = New List(Of SelectExpression)
                    For Each fld As SelectExpression In _fields
                        cl.Add(fld)
                    Next
                End If

                If cl IsNot Nothing Then
                    cl.Sort(Function(fst As SelectExpression, sec As SelectExpression) _
                        fst.ToString.CompareTo(sec.ToString))
                End If
            End If
            Return f
        End Function

        Public Function GetStaticKey(ByVal mgrKey As String, ByVal js As List(Of List(Of OrmJoin)), _
            ByVal fs() As IFilter, ByVal realType As Type, ByVal cb As Cache.CacheListBehavior, _
            ByVal sl As List(Of List(Of SelectExpression)), ByVal realTypeKey As String, _
            ByVal mpe As ObjectMappingEngine) As String
            Dim key As New StringBuilder

            Dim i As Integer = 0
            For Each q As QueryCmd In New QueryIterator(Me)
                If i > 0 Then
                    key.Append("$inner:")
                End If

                Dim f As IFilter = Nothing
                If fs.Length > i Then
                    f = fs(i)
                End If

                If Not q.GetStaticKey(key, js(i), f, If(q._realType Is Nothing, realType, q._realType), cb, sl(i), mpe) Then
                    Return Nothing
                End If
                i += 1
            Next

            key.Append(realTypeKey).Append("$")

            key.Append("$").Append(mgrKey)
            Return key.ToString
        End Function

        Protected Friend Function GetStaticKey(ByVal sb As StringBuilder, ByVal j As IEnumerable(Of OrmJoin), _
            ByVal f As IFilter, ByVal realType As Type, ByVal cb As Cache.CacheListBehavior, _
            ByVal sl As List(Of SelectExpression), ByVal mpe As ObjectMappingEngine) As Boolean
            Dim sb2 As New StringBuilder

            If f IsNot Nothing Then
                Select Case cb
                    Case Cache.CacheListBehavior.CacheAll
                        sb2.Append(f.GetStaticString(mpe)).Append("$")
                    Case Cache.CacheListBehavior.CacheOrThrowException
                        If TryCast(f, IEntityFilter) IsNot Nothing Then
                            sb2.Append(f.GetStaticString(mpe)).Append("$")
                        Else
                            For Each fl As IFilter In f.GetAllFilters
                                Dim dp As Cache.IDependentTypes = Cache.QueryDependentTypes(mpe, fl)
                                If Not Cache.IsCalculated(dp) Then
                                    Throw New ApplicationException
                                End If
                            Next
                        End If
                    Case Cache.CacheListBehavior.CacheWhatCan
                        If TryCast(f, IEntityFilter) IsNot Nothing Then
                            sb2.Append(f.GetStaticString(mpe)).Append("$")
                        Else
                            For Each fl As IFilter In f.GetAllFilters
                                Dim dp As Cache.IDependentTypes = Cache.QueryDependentTypes(mpe, fl)
                                If Not Cache.IsCalculated(dp) Then
                                    Return False
                                End If
                            Next
                        End If
                    Case Else
                        Throw New NotSupportedException(String.Format("Cache behavior {0} is not supported", cb.ToString))
                End Select
            End If

            If j IsNot Nothing Then
                For Each join As OrmJoin In j
                    If Not OrmJoin.IsEmpty(join) Then
                        If join.Table IsNot Nothing Then
                            Select Case cb
                                Case Cache.CacheListBehavior.CacheAll
                                    'do nothing
                                Case Cache.CacheListBehavior.CacheOrThrowException
                                Case Cache.CacheListBehavior.CacheWhatCan
                                    Return False
                                Case Else
                                    Throw New NotSupportedException(String.Format("Cache behavior {0} is not supported", cb.ToString))
                            End Select
                        End If
                        sb2.Append(join.GetStaticString(mpe))
                    End If
                Next
            End If

            If sb2.Length = 0 Then
                If realType IsNot Nothing Then
                    sb2.Append(realType.ToString)
                ElseIf _table IsNot Nothing Then
                    sb2.Append(_table.RawName)
                Else
                    Throw New NotSupportedException
                End If
                sb2.Append("$")
            End If

            sb.Append(sb2.ToString)

            If _rn IsNot Nothing Then
                sb.Append(_rn.ToStaticString(mpe))
            End If

            If _top IsNot Nothing Then
                sb.Append(_top.GetStaticKey).Append("$")
            End If

            sb.Append(_distinct.ToString).Append("$")

            If sl IsNot Nothing Then
                For Each c As SelectExpression In sl
                    If Not GetStaticKeyFromProp(sb, cb, c, mpe) Then
                        Return False
                    End If
                Next
                sb.Append("$")
            End If

            If _order IsNot Nothing Then
                If CacheSort OrElse _top IsNot Nothing Then
                    For Each n As Sort In New Sort.Iterator(_order)
                        If Not GetStaticKeyFromProp(sb, cb, n, mpe) Then
                            Return False
                        End If
                        sb.Append(n.ToString)
                    Next
                    sb.Append("$")
                End If
            End If

            Return True
        End Function

        Private Shared Function GetStaticKeyFromProp(ByVal sb As StringBuilder, ByVal cb As Cache.CacheListBehavior, ByVal c As SelectExpression, ByVal mpe As ObjectMappingEngine) As Boolean
            If c.Type Is Nothing Then
                Dim dp As Cache.IDependentTypes = Cache.QueryDependentTypes(mpe, c)
                If Not Cache.IsCalculated(dp) Then
                    Select Case cb
                        Case Cache.CacheListBehavior.CacheAll
                            'do nothing
                        Case Cache.CacheListBehavior.CacheOrThrowException
                        Case Cache.CacheListBehavior.CacheWhatCan
                            Return False
                        Case Else
                            Throw New NotSupportedException(String.Format("Cache behavior {0} is not supported", cb.ToString))
                    End Select
                End If
            End If
            sb.Append(c.ToString)
            Return True
        End Function

        Public Function GetDynamicKey(ByVal js As List(Of List(Of OrmJoin)), ByVal fs() As IFilter) As String
            Dim id As New StringBuilder

            Dim i As Integer = 0
            For Each q As QueryCmd In New QueryIterator(Me)
                If i > 0 Then
                    id.Append("$inner:")
                End If
                Dim f As IFilter = Nothing
                If fs.Length > i Then
                    f = fs(i)
                End If
                q.GetDynamicKey(id, js(i), f)
                i += 1
            Next

            Return id.ToString '& GetType(T).ToString
        End Function

        Protected Friend Sub GetDynamicKey(ByVal sb As StringBuilder, ByVal j As IEnumerable(Of OrmJoin), ByVal f As IFilter)
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

            'Dim cnt As Integer = 0
            'Dim n As Sort = _order
            'Do While n IsNot Nothing
            '    cnt += 1
            '    n = n.Previous
            'Loop
            'If cnt > 0 Then
            '    sb.Append("sort=").Append(cnt).Append("$")
            'End If

            If _rn IsNot Nothing Then
                sb.Append(_rn.ToString)
            End If
        End Sub

        Public Overrides Function ToString() As String
            Throw New NotSupportedException
        End Function

        Public Function ToStaticString(ByVal mpe As ObjectMappingEngine) As String
            Dim sb As New StringBuilder
            Dim l As List(Of SelectExpression) = Nothing
            If _fields IsNot Nothing Then
                l = New List(Of SelectExpression)(_fields)
            End If
            GetStaticKey(sb, _joins, _filter.Filter, _realType, Cache.CacheListBehavior.CacheAll, l, mpe)
            Return sb.ToString
        End Function


#Region " Properties "
        Public Property EntityName() As String
            Get
                Return _en
            End Get
            Set(ByVal value As String)
                _en = value
            End Set
        End Property

        Public Property OuterQuery() As QueryCmd
            Get
                Return _outer
            End Get
            Set(ByVal value As QueryCmd)
                _outer = value
                RenewMark()
            End Set
        End Property

        Public Const RowNumerColumn As String = "[worm.row_number]"

        Public Property RowNumberFilter() As Worm.Database.Criteria.Core.TableFilter
            Get
                Return _rn
            End Get
            Set(ByVal value As Worm.Database.Criteria.Core.TableFilter)
                _rn = value
                RenewMark()
            End Set
        End Property

        Public Property CacheSort() As Boolean
            Get
                Return _cacheSort
            End Get
            Set(ByVal value As Boolean)
                _cacheSort = value
                RenewMark()
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

        Public ReadOnly Property Mark() As Guid
            Get
                Return _mark
            End Get
        End Property

        Public ReadOnly Property SMark() As Guid
            Get
                Return _statementMark
            End Get
        End Property

        Public Property Hint() As String
            Get
                Return _hint
            End Get
            Set(ByVal value As String)
                _hint = value
                RenewStatementMark()
            End Set
        End Property

        Public ReadOnly Property Table() As SourceFragment
            Get
                Return _table
            End Get
        End Property

        Public Property ClientPaging() As Paging
            Get
                Return _clientPage
            End Get
            Set(ByVal value As Paging)
                _clientPage = value
            End Set
        End Property

        Public Property LiveTime() As TimeSpan
            Get
                Return _liveTime
            End Get
            Set(ByVal value As TimeSpan)
                _liveTime = value
            End Set
        End Property

        Public Property ExternalCacheMark() As String
            Get
                Return _mgrMark
            End Get
            Set(ByVal value As String)
                If Not String.IsNullOrEmpty(_mgrMark) Then
                    Throw New InvalidOperationException
                End If
                _mgrMark = value
                _resDic = True
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
                RenewMark()
            End Set
        End Property

        Public Property Group() As ObjectModel.ReadOnlyCollection(Of Grouping)
            Get
                Return _group
            End Get
            Set(ByVal value As ObjectModel.ReadOnlyCollection(Of Grouping))
                _group = value
                RenewMark()
            End Set
        End Property

        Public Property Joins() As OrmJoin()
            Get
                Return _joins
            End Get
            Set(ByVal value As OrmJoin())
                _joins = value
                RenewMark()
            End Set
        End Property

        Public Property Aggregates() As ObjectModel.ReadOnlyCollection(Of AggregateBase)
            Get
                Return _aggregates
            End Get
            Set(ByVal value As ObjectModel.ReadOnlyCollection(Of AggregateBase))
                _aggregates = value
                RenewMark()
            End Set
        End Property

        Public Property SelectList() As ObjectModel.ReadOnlyCollection(Of SelectExpression)
            Get
                Return _fields
            End Get
            Set(ByVal value As ObjectModel.ReadOnlyCollection(Of SelectExpression))
                _fields = value
                RenewMark()
            End Set
        End Property

        Public Property propSort() As Sort
            Get
                Return _order
            End Get
            Set(ByVal value As Sort)
                _order = value
                RenewMark()
                'AddHandler value.OnChange, AddressOf OnSortChanged
            End Set
        End Property

        Public Property propTop() As Top
            Get
                Return _top
            End Get
            Set(ByVal value As Top)
                _top = value
                RenewMark()
            End Set
        End Property

        Public Property propDistinct() As Boolean
            Get
                Return _distinct
            End Get
            Set(ByVal value As Boolean)
                _distinct = value
                RenewMark()
            End Set
        End Property

        Public Property Filter() As IGetFilter
            Get
                Return _filter
            End Get
            Set(ByVal value As IGetFilter)
                _filter = value
                RenewMark()
            End Set
        End Property

        Public Property propWithLoad() As Boolean
            Get
                Return _load
            End Get
            Set(ByVal value As Boolean)
                _load = value
                If _o Is Nothing Then
                    RenewStatementMark()
                Else
                    RenewMark()
                End If
            End Set
        End Property
#End Region

        Public Function Distinct(ByVal value As Boolean) As QueryCmd
            propDistinct = value
            Return Me
        End Function

        Public Function Top(ByVal value As Integer) As QueryCmd
            propTop = New Query.Top(value)
            Return Me
        End Function

        Public Function Sort(ByVal value As Sort) As QueryCmd
            propSort = value
            Return Me
        End Function

        Public Function WithLoad(ByVal value As Boolean) As QueryCmd
            propWithLoad = value
            Return Me
        End Function

        Public Function Where(ByVal filter As IGetFilter) As QueryCmd
            Me.Filter = filter
            Return Me
        End Function

        Public Function From(ByVal t As SourceFragment) As QueryCmd
            _table = t
            RenewMark()
            Return Me
        End Function

        Public Function [Select](ByVal fields() As SelectExpression) As QueryCmd
            SelectList = New ObjectModel.ReadOnlyCollection(Of SelectExpression)(fields)
            Return Me
        End Function

        Public Function GroupBy(ByVal fields() As Grouping) As QueryCmd
            Group = New ObjectModel.ReadOnlyCollection(Of Grouping)(fields)
            Return Me
        End Function

        Public Function [SelectAgg](ByVal aggrs() As AggregateBase) As QueryCmd
            Aggregates = New ObjectModel.ReadOnlyCollection(Of AggregateBase)(aggrs)
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

        'Public Function ToListTypeless(ByVal mgr As OrmManager) As IList
        '    Dim q As QueryCmdBase = CreateTypedCopy(SelectedType)
        '    Dim e As IList = CType(q.GetType.InvokeMember("_Exec", Reflection.BindingFlags.Instance Or Reflection.BindingFlags.NonPublic Or Reflection.BindingFlags.InvokeMethod, _
        '                           Nothing, q, New Object() {mgr}), System.Collections.IList)
        '    Return e
        'End Function

        Private Class cls
            Private _m As CreateManagerDelegate
            Private _gm As ICreateManager

            Public Sub New(ByVal getMgr As CreateManagerDelegate)
                _m = getMgr
            End Sub

            Public Sub New(ByVal getMgr As ICreateManager)
                _gm = getMgr
            End Sub

            'Protected Sub GetManager(ByVal o As IEntity, ByVal args As ManagerRequiredArgs)
            '    If _m Is Nothing Then
            '        args.Manager = _gm.CreateManager
            '    Else
            '        args.Manager = _m()
            '    End If
            'End Sub

            Public Sub ObjectCreated(ByVal o As ICachedEntity, ByVal mgr As OrmManager)
                'AddHandler o.ManagerRequired, AddressOf GetManager
                If _m Is Nothing Then
                    o.SetCreateManager(_gm)
                Else
                    o.SetCreateManager(New CreateManager(_m))
                End If
            End Sub
        End Class

        Public Function ToList(ByVal mgr As OrmManager) As IList
            Dim t As MethodInfo = Me.GetType.GetMethod("ToEntityList", New Type() {GetType(OrmManager)})
            t = t.MakeGenericMethod(New Type() {SelectedType})
            Return CType(t.Invoke(Me, New Object() {mgr}), System.Collections.IList)
        End Function

        Public Function ToEntityList(Of T As {_ICachedEntity})(ByVal getMgr As CreateManagerDelegate) As ReadOnlyEntityList(Of T)
            Dim mgr As OrmManager = getMgr()
            Try
                mgr.RaiseObjectCreation = True
                AddHandler mgr.ObjectCreated, AddressOf New cls(getMgr).ObjectCreated
                'AddHandler mgr.ObjectCreated, Function(o As ICachedEntity, m As OrmManager) AddHandler o.ManagerRequired,function(ByVal o As IEntity, ByVal args As ManagerRequiredArgs) args.Manager = getmgr)
                Return ToEntityList(Of T)(mgr)
            Finally
                If mgr IsNot Nothing Then
                    mgr.Dispose()
                End If
            End Try
        End Function

        Public Function ToEntityList(Of T As {_ICachedEntity})(ByVal getMgr As ICreateManager) As ReadOnlyEntityList(Of T)
            Using mgr As OrmManager = getMgr.CreateManager
                mgr.RaiseObjectCreation = True
                AddHandler mgr.ObjectCreated, AddressOf New cls(getMgr).ObjectCreated
                'AddHandler mgr.ObjectCreated, Function(o As ICachedEntity, m As OrmManager) AddHandler o.ManagerRequired,function(ByVal o As IEntity, ByVal args As ManagerRequiredArgs) args.Manager = getmgr)
                Return ToEntityList(Of T)(mgr)
            End Using
        End Function

        Public Function ToEntityList(Of T As {_ICachedEntity})(ByVal mgr As OrmManager) As ReadOnlyEntityList(Of T)
            Return GetExecutor(mgr).Exec(Of T)(mgr, Me)
        End Function

        Public Function ToEntityList(Of T As _ICachedEntity)() As ReadOnlyEntityList(Of T)
            If _getMgr Is Nothing Then
                Throw New InvalidOperationException("OrmManager required")
            End If

            Return ToEntityList(Of T)(_getMgr)
        End Function

        Private Sub SetCT2Nothing()
            _createType = Nothing
        End Sub

        Public Function ToObjectList(Of T As _IEntity)(ByVal mgr As OrmManager) As ReadOnlyObjectList(Of T)
            If GetType(AnonymousEntity).IsAssignableFrom(GetType(T)) AndAlso _createType Is Nothing Then
                _createType = GetType(T)
                Using New OnExitScopeAction(AddressOf SetCT2Nothing)
                    Return GetExecutor(mgr).ExecEntity(Of T)(mgr, Me)
                End Using
            Else
                Return GetExecutor(mgr).ExecEntity(Of T)(mgr, Me)
            End If
        End Function

        Public Function ToOrmList(Of T As {_IOrmBase})(ByVal mgr As OrmManager) As ReadOnlyList(Of T)
            Return CType(ToEntityList(Of T)(mgr), ReadOnlyList(Of T))
        End Function

        Public Function ToOrmList(Of T As {_IOrmBase})(ByVal getMgr As ICreateManager) As ReadOnlyList(Of T)
            Using mgr As OrmManager = getMgr.CreateManager
                mgr.RaiseObjectCreation = True
                AddHandler mgr.ObjectCreated, AddressOf New cls(getMgr).ObjectCreated
                Return ToOrmList(Of T)(mgr)
            End Using
        End Function

        Public Function ToOrmList(Of T As {_IOrmBase})(ByVal getMgr As CreateManagerDelegate) As ReadOnlyList(Of T)
            Using mgr As OrmManager = getMgr()
                mgr.RaiseObjectCreation = True
                AddHandler mgr.ObjectCreated, AddressOf New cls(getMgr).ObjectCreated
                Return ToOrmList(Of T)(mgr)
            End Using
        End Function

        Public Function ToOrmList(Of T As _IOrmBase)() As ReadOnlyList(Of T)
            If _getMgr Is Nothing Then
                Throw New InvalidOperationException("OrmManager required")
            End If

            Return ToOrmList(Of T)(_getMgr)
        End Function

        Public Function ToList(Of T As {_ICachedEntity})(ByVal mgr As OrmManager) As IList(Of T)
            Return ToEntityList(Of T)(mgr)
        End Function

        Public Function ToList(Of SelectType As {_ICachedEntity, New}, ReturnType As {_ICachedEntity})(ByVal mgr As OrmManager) As IList(Of ReturnType)
            Return GetExecutor(mgr).Exec(Of SelectType, ReturnType)(mgr, Me)
        End Function

        'Public Function ExecTypeless(ByVal mgr As OrmManager) As IEnumerator
        '    Return ToListTypeless(mgr).GetEnumerator
        'End Function

        Public Function [Single](Of T)(ByVal mgr As OrmManager) As T
            Dim l As IList(Of T) = ToSimpleList(Of T)(mgr)
            If l.Count <> 1 Then
                Throw New InvalidOperationException("Number of items is " & l.Count)
            End If
            Return l(0)
        End Function

        Public Function [SingleOrDefault](Of T)(ByVal mgr As OrmManager) As T
            Dim l As IList(Of T) = ToSimpleList(Of T)(mgr)
            If l.Count <> 1 Then
                Throw New InvalidOperationException("Number of items is " & l.Count)
            ElseIf l.Count = 1 Then
                Return l(0)
            End If
            Return Nothing
        End Function

        Public Function SingleEntity(Of T As _ICachedEntity)(ByVal mgr As OrmManager) As T
            Dim l As ReadOnlyEntityList(Of T) = ToEntityList(Of T)(mgr)
            If l.Count <> 1 Then
                Throw New InvalidOperationException("Number of items is " & l.Count)
            End If
            Return l(0)
        End Function

        Public Function SingleOrm(Of T As _IOrmBase)(ByVal mgr As OrmManager) As T
            Dim l As ReadOnlyList(Of T) = ToOrmList(Of T)(mgr)
            If l.Count <> 1 Then
                Throw New InvalidOperationException("Number of items is " & l.Count)
            End If
            Return l(0)
        End Function

        Public Function SingleOrDefaultOrm(Of T As _IOrmBase)(ByVal mgr As OrmManager) As T
            Dim l As ReadOnlyList(Of T) = ToOrmList(Of T)(mgr)
            If l.Count > 1 Then
                Throw New InvalidOperationException("Number of items is " & l.Count)
            ElseIf l.Count = 1 Then
                Return l(0)
            End If
            Return Nothing
        End Function

        Public Function ToSimpleList(Of T)(ByVal mgr As OrmManager) As IList(Of T)
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

        Public Function ToCustomList(Of T As {New, Class})() As IList(Of T)
            If _getMgr Is Nothing Then
                Throw New InvalidOperationException("OrmManager required")
            End If

            Using mgr As OrmManager = _getMgr.CreateManager
                mgr.RaiseObjectCreation = True
                AddHandler mgr.ObjectCreated, AddressOf New cls(_getMgr).ObjectCreated
                Return ToCustomList(Of T)(mgr)
            End Using
        End Function

        Public Function ToCustomList(Of T As {New, Class})(ByVal mgr As OrmManager) As IList(Of T)
            Dim rt As Type = GetType(T)
            Dim mpe As ObjectMappingEngine = mgr.MappingEngine

            Dim hasPK As Boolean
            Dim schema As IObjectSchemaBase = GetSchema(mpe, rt, hasPK)

            Dim l As IEnumerable = Nothing
            Dim r As New List(Of T)

            If hasPK Then
                l = ToObjectList(Of AnonymousCachedEntity)(mgr)
            Else
                l = ToObjectList(Of AnonymousEntity)(mgr)
            End If

            For Each kv As KeyValuePair(Of ColumnAttribute, Reflection.PropertyInfo) In mpe.GetProperties(rt, schema)
                Dim col As ColumnAttribute = kv.Key
                Dim pi As Reflection.PropertyInfo = kv.Value
                For Each e As IEntity In l
                    Dim ro As New T
                    Dim v As Object = e.GetValueOptimized(Nothing, col, Nothing)
                    pi.SetValue(ro, v, Nothing)
                    r.Add(ro)
                Next
            Next

            Return r
        End Function

        Private Function GetSchema(ByVal mpe As ObjectMappingEngine, ByVal t As Type, ByRef pk As Boolean) As IObjectSchemaBase

        End Function

        Public Sub Reset(Of ReturnType As _IEntity)(ByVal mgr As OrmManager)
            GetExecutor(mgr).ResetEntity(Of ReturnType)(mgr, Me)
        End Sub

        Public Sub Renew(Of ReturnType As _ICachedEntity)(ByVal mgr As OrmManager)
            GetExecutor(mgr).Reset(Of ReturnType)(mgr, Me)
        End Sub

        Public Sub Renew(Of SelectType As {_ICachedEntity, New}, ReturnType As _ICachedEntity)(ByVal mgr As OrmManager)
            GetExecutor(mgr).Reset(Of SelectType, ReturnType)(mgr, Me)
        End Sub

        Public Sub CopyTo(ByVal o As QueryCmd)
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
                ._statementMark = _statementMark
                ._realType = _realType
                ._table = _table
                ._top = _top
                ._rn = _rn
                ._outer = _outer
                ._er = _er
                ._en = _en
                ._resDic = _resDic
                ._appendMain = _appendMain
                ._getMgr = _getMgr
                ._createType = _createType
                ._liveTime = _liveTime
                ._mgrMark = _mgrMark
                ._name = _name
                ._execCnt = _execCnt
                ._schema = _schema
                ._cacheSort = _cacheSort
            End With
        End Sub

        Public Function Clone() As Object Implements System.ICloneable.Clone
            Dim q As New QueryCmd
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

        Public Function [Get](ByVal mpe As ObjectMappingEngine) As Cache.IDependentTypes Implements Cache.IQueryDependentTypes.Get
            'If SelectedType Is Nothing Then
            '    Return New Cache.EmptyDependentTypes
            'End If

            Dim dp As New Cache.DependentTypes
            If _joins IsNot Nothing Then
                For Each j As OrmJoin In _joins
                    Dim t As Type = j.Type
                    If t Is Nothing AndAlso Not String.IsNullOrEmpty(j.EntityName) Then
                        t = mpe.GetTypeByEntityName(j.EntityName)
                    End If
                    'If t Is Nothing Then
                    '    Return New Cache.EmptyDependentTypes
                    'End If
                    If t IsNot Nothing Then
                        dp.AddBoth(t)
                    End If
                Next
            End If

            If SelectedType IsNot Nothing AndAlso Not dp.IsEmpty Then
                dp.AddBoth(SelectedType)
            End If

            If _filter IsNot Nothing AndAlso TryCast(_filter, IEntityFilter) Is Nothing Then
                For Each f As IFilter In _filter.Filter.GetAllFilters
                    Dim fdp As Cache.IDependentTypes = Cache.QueryDependentTypes(mpe, f)
                    If Cache.IsCalculated(fdp) Then
                        If SelectedType IsNot Nothing Then
                            dp.AddBoth(SelectedType)
                        End If
                        dp.Merge(fdp)
                        'Else
                        '    Return fdp
                    End If
                Next
            End If

            If _order IsNot Nothing Then
                For Each s As Sort In New Sort.Iterator(_order)
                    Dim fdp As Cache.IDependentTypes = Cache.QueryDependentTypes(mpe, s)
                    If Cache.IsCalculated(fdp) Then
                        If SelectedType IsNot Nothing Then
                            dp.AddUpdated(SelectedType)
                        End If
                        dp.Merge(fdp)
                        'Else
                        '    Return fdp
                    End If
                Next
            End If

            If _fields IsNot Nothing Then
                For Each f As SelectExpression In _fields
                    Dim fdp As Cache.IDependentTypes = Cache.QueryDependentTypes(mpe, f)
                    If Cache.IsCalculated(fdp) Then
                        dp.Merge(fdp)
                        'Else
                        '    Return fdp
                    End If
                Next
            End If
            Return dp.Get
        End Function

        Public Function GetOrmCommand(Of T As _IOrmBase)(ByVal mgr As OrmManager) As OrmQueryCmd(Of T)
            'Dim f As ICreateQueryCmd = TryCast(mgr, ICreateQueryCmd)
            Dim q As New OrmQueryCmd(Of T)()
            CopyTo(q)
            If _getMgr Is Nothing Then
                q.Exec(mgr)
            End If
            Return q
        End Function

        Public Function GetOrmCommand(Of T As _IOrmBase)() As OrmQueryCmd(Of T)
            Dim mgr As OrmManager = OrmManager.CurrentManager
            Return GetOrmCommand(Of T)(mgr)
        End Function

#Region " Create methods "

        Public Shared Function Create(ByVal table As SourceFragment, ByVal mgr As OrmManager) As QueryCmd
            Dim f As ICreateQueryCmd = TryCast(mgr, ICreateQueryCmd)
            Dim q As QueryCmd = Nothing
            If f Is Nothing Then
                q = New QueryCmd(table)
            Else
                q = f.Create(table)
            End If
            Dim cm As ICreateManager = TryCast(mgr, ICreateManager)
            If cm IsNot Nothing Then
                q._getMgr = cm
            End If
            Return q
        End Function

        Public Shared Function CreateAndGetOrmCommand(Of T As _IOrmBase)(ByVal mgr As OrmManager) As OrmQueryCmd(Of T)
            Dim selectType As Type = GetType(T)
            Return Create(selectType, mgr).GetOrmCommand(Of T)(mgr)
        End Function

        Public Shared Function Create(ByVal selectType As Type, ByVal mgr As OrmManager) As QueryCmd
            Dim f As ICreateQueryCmd = TryCast(mgr, ICreateQueryCmd)
            Dim q As QueryCmd = Nothing
            If f Is Nothing Then
                q = New QueryCmd(selectType)
            Else
                q = f.Create(selectType)
            End If
            Dim cm As ICreateManager = TryCast(mgr, ICreateManager)
            If cm IsNot Nothing Then
                q._getMgr = cm
            End If
            Return q
        End Function

        Public Shared Function CreateByEntityName(ByVal entityName As String, ByVal mgr As OrmManager) As QueryCmd
            Dim f As ICreateQueryCmd = TryCast(mgr, ICreateQueryCmd)
            Dim q As QueryCmd = Nothing
            If f Is Nothing Then
                q = New QueryCmd(entityName)
            Else
                q = f.CreateByEntityName(entityName)
            End If
            Dim cm As ICreateManager = TryCast(mgr, ICreateManager)
            If cm IsNot Nothing Then
                q._getMgr = cm
            End If
            Return q
        End Function

        Public Shared Function Create(ByVal obj As _IOrmBase, ByVal mgr As OrmManager) As QueryCmd
            Dim f As ICreateQueryCmd = TryCast(mgr, ICreateQueryCmd)
            Dim q As QueryCmd = Nothing
            If f Is Nothing Then
                q = New QueryCmd(obj)
            Else
                q = f.Create(obj)
            End If
            Dim cm As ICreateManager = TryCast(mgr, ICreateManager)
            If cm IsNot Nothing Then
                q._getMgr = cm
            End If
            Return q
        End Function

        Public Shared Function Create(ByVal obj As _IOrmBase, ByVal key As String, ByVal mgr As OrmManager) As QueryCmd
            Dim f As ICreateQueryCmd = TryCast(mgr, ICreateQueryCmd)
            Dim q As QueryCmd = Nothing
            If f Is Nothing Then
                q = New QueryCmd(obj, key)
            Else
                q = f.Create(obj, key)
            End If
            Dim cm As ICreateManager = TryCast(mgr, ICreateManager)
            If cm IsNot Nothing Then
                q._getMgr = cm
            End If
            Return q
        End Function

        Public Shared Function Create(ByVal name As String, ByVal table As SourceFragment, ByVal mgr As OrmManager) As QueryCmd
            Dim f As ICreateQueryCmd = TryCast(mgr, ICreateQueryCmd)
            Dim q As QueryCmd = Nothing
            If f Is Nothing Then
                q = New QueryCmd(table)
                q.Name = name
            Else
                q = f.Create(name, table)
            End If
            Dim cm As ICreateManager = TryCast(mgr, ICreateManager)
            If cm IsNot Nothing Then
                q._getMgr = cm
            End If
            Return q
        End Function

        Public Shared Function CreateAndGetOrmCommand(Of T As _IOrmBase)(ByVal name As String, ByVal mgr As OrmManager) As OrmQueryCmd(Of T)
            Dim selectType As Type = GetType(T)
            Return Create(name, selectType, mgr).GetOrmCommand(Of T)(mgr)
        End Function

        Public Shared Function Create(ByVal name As String, ByVal selectType As Type, ByVal mgr As OrmManager) As QueryCmd
            Dim f As ICreateQueryCmd = TryCast(mgr, ICreateQueryCmd)
            Dim q As QueryCmd = Nothing
            If f Is Nothing Then
                q = New QueryCmd(selectType)
                q.Name = name
            Else
                q = f.Create(name, selectType)
            End If
            Dim cm As ICreateManager = TryCast(mgr, ICreateManager)
            If cm IsNot Nothing Then
                q._getMgr = cm
            End If
            Return q
        End Function

        Public Shared Function CreateByEntityName(ByVal name As String, ByVal entityName As String, ByVal mgr As OrmManager) As QueryCmd
            Dim f As ICreateQueryCmd = TryCast(mgr, ICreateQueryCmd)
            Dim q As QueryCmd = Nothing
            If f Is Nothing Then
                q = New QueryCmd(entityName)
                q.Name = name
            Else
                q = f.CreateByEntityName(name, entityName)
            End If
            Dim cm As ICreateManager = TryCast(mgr, ICreateManager)
            If cm IsNot Nothing Then
                q._getMgr = cm
            End If
            Return q
        End Function

        Public Shared Function Create(ByVal name As String, ByVal obj As _IOrmBase, ByVal mgr As OrmManager) As QueryCmd
            Dim f As ICreateQueryCmd = TryCast(mgr, ICreateQueryCmd)
            Dim q As QueryCmd = Nothing
            If f Is Nothing Then
                q = New QueryCmd(obj)
                q.Name = name
            Else
                q = f.Create(name, obj)
            End If
            Dim cm As ICreateManager = TryCast(mgr, ICreateManager)
            If cm IsNot Nothing Then
                q._getMgr = cm
            End If
            Return q
        End Function

        Public Shared Function Create(ByVal name As String, ByVal obj As _IOrmBase, ByVal key As String, ByVal mgr As OrmManager) As QueryCmd
            Dim f As ICreateQueryCmd = TryCast(mgr, ICreateQueryCmd)
            Dim q As QueryCmd = Nothing
            If f Is Nothing Then
                q = New QueryCmd(obj, key)
                q.Name = name
            Else
                q = f.Create(name, obj, key)
            End If
            Dim cm As ICreateManager = TryCast(mgr, ICreateManager)
            If cm IsNot Nothing Then
                q._getMgr = cm
            End If
            Return q
        End Function

        Public Shared Function Create(ByVal table As SourceFragment) As QueryCmd
            Return Create(table, OrmManager.CurrentManager)
        End Function

        Public Shared Function CreateAndGetOrmCommand(Of T As _IOrmBase)() As OrmQueryCmd(Of T)
            Return CreateAndGetOrmCommand(Of T)(OrmManager.CurrentManager)
        End Function

        Public Shared Function Create(ByVal selectType As Type) As QueryCmd
            Return Create(selectType, OrmManager.CurrentManager)
        End Function

        Public Shared Function CreateByEntityName(ByVal entityName As String) As QueryCmd
            Return CreateByEntityName(entityName, OrmManager.CurrentManager)
        End Function

        Public Shared Function Create(ByVal obj As _IOrmBase) As QueryCmd
            Return Create(obj, OrmManager.CurrentManager)
        End Function

        Public Shared Function Create(ByVal obj As _IOrmBase, ByVal key As String) As QueryCmd
            Return Create(obj, key, OrmManager.CurrentManager)
        End Function

        Public Shared Function Create(ByVal name As String, ByVal table As SourceFragment) As QueryCmd
            Return Create(name, table, OrmManager.CurrentManager)
        End Function

        Public Shared Function CreateAndGetOrmCommand(Of T As _IOrmBase)(ByVal name As String) As OrmQueryCmd(Of T)
            Return CreateAndGetOrmCommand(Of T)(name, OrmManager.CurrentManager)
        End Function

        Public Shared Function Create(ByVal name As String, ByVal selectType As Type) As QueryCmd
            Return Create(name, selectType, OrmManager.CurrentManager)
        End Function

        Public Shared Function CreateByEntityName(ByVal name As String, ByVal entityName As String) As QueryCmd
            Return CreateByEntityName(name, entityName, OrmManager.CurrentManager)
        End Function

        Public Shared Function Create(ByVal name As String, ByVal obj As _IOrmBase) As QueryCmd
            Return Create(name, obj, OrmManager.CurrentManager)
        End Function

        Public Shared Function Create(ByVal name As String, ByVal obj As _IOrmBase, ByVal key As String) As QueryCmd
            Return Create(name, obj, key, OrmManager.CurrentManager)
        End Function

#End Region

        Public Function _ToString() As String Implements Criteria.Values.IQueryElement._ToString
            Dim sb As New StringBuilder
            GetDynamicKey(sb, _joins, _filter.Filter)
            Return sb.ToString
        End Function

        Public Function GetStaticString(ByVal mpe As ObjectMappingEngine) As String Implements Criteria.Values.IQueryElement.GetStaticString
            Return ToStaticString(mpe)
        End Function
    End Class

    Public Class OrmQueryCmd(Of T As _IOrmBase)
        Inherits QueryCmd
        Implements Generic.IEnumerable(Of T)

        Private _preCmp As ReadOnlyList(Of T)
        Private _oldMark As Guid

        Public Sub Exec(ByVal mgr As OrmManager)
            _preCmp = ToOrmList(Of T)(mgr)
            _oldMark = _mark
        End Sub

        Public Shared Widening Operator CType(ByVal cmd As OrmQueryCmd(Of T)) As ReadOnlyList(Of T)
            If cmd._getMgr IsNot Nothing Then
                Return cmd.ToOrmList(Of T)(cmd._getMgr)
            ElseIf cmd._preCmp IsNot Nothing AndAlso cmd._oldMark = cmd._mark Then
                Return cmd._preCmp
            Else
                Throw New InvalidOperationException("Cannot convert to list")
            End If
        End Operator

#Region " Ctors "
        Public Sub New()
        End Sub

        Public Sub New(ByVal table As SourceFragment)
            MyBase.New(table)
        End Sub

        Public Sub New(ByVal selectType As Type)
            MyBase.New(selectType)
        End Sub

        Public Sub New(ByVal entityName As String)
            MyBase.New(entityName)
        End Sub

        Public Sub New(ByVal obj As _IOrmBase)
            MyBase.New(obj)
        End Sub

        Public Sub New(ByVal obj As _IOrmBase, ByVal key As String)
            MyBase.New(obj, key)
        End Sub

        Public Sub New(ByVal table As SourceFragment, ByVal getMgr As ICreateManager)
            MyBase.New(table, getMgr)
        End Sub

        Public Sub New(ByVal selectType As Type, ByVal getMgr As ICreateManager)
            MyBase.New(selectType, getMgr)
        End Sub

        Public Sub New(ByVal entityName As String, ByVal getMgr As ICreateManager)
            MyBase.New(entityName, getMgr)
        End Sub

        Public Sub New(ByVal obj As _IOrmBase, ByVal getMgr As ICreateManager)
            MyBase.New(obj, getMgr)
        End Sub

        Public Sub New(ByVal obj As _IOrmBase, ByVal key As String, ByVal getMgr As ICreateManager)
            MyBase.New(obj, key, getMgr)
        End Sub

#End Region

        Public Function GetEnumerator() As System.Collections.Generic.IEnumerator(Of T) Implements System.Collections.Generic.IEnumerable(Of T).GetEnumerator
            Return CType(Me, ReadOnlyList(Of T)).GetEnumerator
        End Function

        Protected Function _GetEnumerator() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
            Return GetEnumerator()
        End Function

        Public Overrides Property SelectedType() As System.Type
            Get
                If MyBase.SelectedType Is Nothing Then
                    Return GetType(T)
                Else
                    Return MyBase.SelectedType
                End If
            End Get
            Set(ByVal value As System.Type)
                MyBase.SelectedType = value
            End Set
        End Property
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

    '    Public Function Exec(ByVal mgr As OrmManager) As ReadOnlyEntityList(Of ReturnType)
    '        Return GetExecutor(mgr).Exec(Of ReturnType)(mgr, Me)
    '    End Function

    '    Public Function [Single](ByVal mgr As OrmManager) As ReturnType
    '        Dim r As ReadOnlyEntityList(Of ReturnType) = Exec(mgr)
    '        If r.Count <> 1 Then
    '            Throw New InvalidOperationException
    '        Else
    '            Return r(0)
    '        End If
    '    End Function

    '    Public Function Exec(Of T)(ByVal mgr As OrmManager) As IList(Of T)
    '        Return GetExecutor(mgr).ExecSimple(Of ReturnType, T)(mgr, Me)
    '    End Function
    'End Class

    'Public Class QueryEntity(Of ReturnType As {_IEntity, New})
    '    Inherits QueryCmdBase

    '    'Protected Function _Exec(ByVal mgr As OrmManager) As ReadOnlyList(Of ReturnType)
    '    '    Return GetExecutor(mgr).Exec(Of ReturnType)(mgr, Me)
    '    'End Function

    '    Public Function Exec(ByVal mgr As OrmManager) As ReadOnlyObjectList(Of ReturnType)
    '        Return GetExecutor(mgr).ExecEntity(Of ReturnType)(mgr, Me)
    '    End Function

    '    'Public Function Exec(Of T)(ByVal mgr As OrmManager) As IList(Of T)
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

    '    Public Function [Single](ByVal mgr As OrmManager) As ReturnType
    '        Dim r As ReadOnlyObjectList(Of ReturnType) = Exec(mgr)
    '        If r.Count <> 1 Then
    '            Throw New InvalidOperationException
    '        Else
    '            Return r(0)
    '        End If
    '    End Function

    'End Class

End Namespace