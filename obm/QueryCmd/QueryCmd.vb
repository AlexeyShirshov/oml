Imports System.Collections.Generic
Imports Worm.Entities.Meta
Imports Worm.Criteria.Core
Imports Worm.Sorting
Imports Worm.Entities
Imports Worm.Criteria.Joins
Imports System.Reflection
Imports Worm.Criteria.Conditions

Namespace Query

    Public Class QueryCmd
        Implements ICloneable, Cache.IQueryDependentTypes, Criteria.Values.IQueryElement

#Region " Classes "
        Class FromClause
            Public ObjectSource As ObjectSource
            Public Table As SourceFragment
            Public Query As QueryCmd

            Public Sub New(ByVal table As SourceFragment)
                Me.Table = table
            End Sub

            Public Sub New(ByVal query As QueryCmd)
                Me.Query = query
            End Sub

            Public Sub New(ByVal [alias] As ObjectAlias)
                Me.ObjectSource = New ObjectSource([alias])
            End Sub

            Public Sub New(ByVal t As Type)
                Me.ObjectSource = New ObjectSource(t)
            End Sub

            Public Sub New(ByVal entityName As String)
                Me.ObjectSource = New ObjectSource(entityName)
            End Sub

            Public Sub New(ByVal os As ObjectSource)
                Me.ObjectSource = os
            End Sub
        End Class

        Public Class CacheDictionaryRequiredEventArgs
            Inherits EventArgs

            Private _del As GetDictionaryDelegate
            Public Property GetDictionary() As GetDictionaryDelegate
                Get
                    Return _del
                End Get
                Set(ByVal value As GetDictionaryDelegate)
                    _del = value
                End Set
            End Property
        End Class

        Public Class ExternalDictionaryEventArgs
            Inherits EventArgs

            Private _dic As IDictionary
            Private _key As String

            Public Sub New(ByVal key As String)
                _key = key
            End Sub

            Public ReadOnly Property Key() As String
                Get
                    Return _key
                End Get
            End Property

            Public Property Dictionary() As IDictionary
                Get
                    Return _dic
                End Get
                Set(ByVal value As IDictionary)
                    _dic = value
                End Set
            End Property
        End Class

        Class svct
            Private _oldct As ObjectSource
            Private _types As ObjectModel.ReadOnlyCollection(Of Pair(Of ObjectSource, Boolean))
            Private _cmd As QueryCmd

            Sub New(ByVal cmd As QueryCmd)
                _oldct = cmd._createType
                If cmd._types IsNot Nothing Then
                    _types = New ObjectModel.ReadOnlyCollection(Of Pair(Of ObjectSource, Boolean))(cmd._types)
                End If
                _cmd = cmd
            End Sub

            Public Sub SetCT2Nothing()
                _cmd._createType = _oldct
                _cmd._types = _types
            End Sub
        End Class

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

            Public Sub ObjectCreated(ByVal mgr As OrmManager, ByVal o As IEntity)
                'AddHandler o.ManagerRequired, AddressOf GetManager
                If _m Is Nothing Then
                    CType(o, _IEntity).SetCreateManager(_gm)
                Else
                    CType(o, _IEntity).SetCreateManager(New CreateManager(_m))
                End If
            End Sub
        End Class

#End Region

        Public Delegate Function GetDictionaryDelegate(ByVal key As String) As IDictionary

        Public Event CacheDictionaryRequired(ByVal sender As QueryCmd, ByVal args As CacheDictionaryRequiredEventArgs)
        Public Event ExternalDictionary(ByVal sender As QueryCmd, ByVal args As ExternalDictionaryEventArgs)

        Protected _fields As ObjectModel.ReadOnlyCollection(Of SelectExpression)
        Protected _types As ObjectModel.ReadOnlyCollection(Of Pair(Of ObjectSource, Boolean))
        Protected _filter As IGetFilter
        Protected _group As ObjectModel.ReadOnlyCollection(Of Grouping)
        Protected _order As Sort
        Protected _aggregates As ObjectModel.ReadOnlyCollection(Of AggregateBase)
        'Protected Friend _load As Boolean
        Protected _top As Top
        Protected _page As Nullable(Of Integer)
        Protected _distinct As Boolean
        Protected _dontcache As Boolean
        Private _liveTime As TimeSpan
        Private _mgrMark As String
        Protected _clientPage As Paging
        Protected _joins() As QueryJoin
        Protected _autoJoins As Boolean
        Protected _from As FromClause
        Protected _hint As String
        Protected _mark As Guid = Guid.NewGuid 'Environment.TickCount
        Protected _statementMark As Guid = Guid.NewGuid 'Environment.TickCount
        'Protected _returnType As Type
        'Protected _realType As Type
        Private _m2mObject As IKeyEntity
        Protected _m2mKey As String
        Protected _rn As TableFilter
        'Protected _outer As QueryCmd
        Private _er As OrmManager.ExecutionResult
        'Private _selectSrc As ObjectSource
        Friend _resDic As Boolean
        Private _appendMain As Boolean?
        Protected _getMgr As ICreateManager
        Private _name As String
        Private _execCnt As Integer
        Private _schema As ObjectMappingEngine
        Private _cacheSort As Boolean
        Private _autoFields As Boolean
        Private _timeout As Nullable(Of Integer)
        Private _oschema As IEntitySchema
        Private _createType As ObjectSource

#Region " Cache "
        Private _dic As IDictionary(Of ObjectSource, IEntitySchema)
#End Region

        Public ReadOnly Property CreateType() As ObjectSource
            Get
                Return _createType
            End Get
            'Set(ByVal value As Type)
            '    If _createType IsNot Nothing AndAlso _execCnt > 0 Then
            '        Throw New QueryCmdException("Cannot change CreateType", Me)
            '    End If
            '    _createType = value
            'End Set
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

        Protected Friend Property Obj() As IKeyEntity
            Get
                Return _m2mObject
            End Get
            Set(ByVal value As IKeyEntity)
                _m2mObject = value
                RenewMark()
            End Set
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

        Protected Friend Sub RaiseExternalDictionary(ByVal args As ExternalDictionaryEventArgs)
            RaiseEvent ExternalDictionary(Me, args)
        End Sub

        'Protected Friend Sub SetSelectList(ByVal l As ObjectModel.ReadOnlyCollection(Of OrmProperty))
        '    _fields = l
        'End Sub

        Private _exec As IExecutor

        Public Function GetExecutor(ByVal mgr As OrmManager) As IExecutor
            'If _dontcache Then
            If _exec Is Nothing Then
                _exec = mgr.StmtGenerator.CreateExecutor()
            End If
            Return _exec
            'Else
            'Return mgr.ObjectSchema.CreateExecutor()
            'End If
        End Function

        Protected Friend ReadOnly Property AppendMain() As Boolean?
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

        Public Sub New(ByVal getMgr As CreateManagerDelegate)
            _getMgr = New CreateManager(getMgr)
        End Sub

        Public Sub New(ByVal getMgr As ICreateManager)
            _getMgr = getMgr
        End Sub

        'Public Sub New(ByVal table As SourceFragment)
        '    _from = New FromClause(table)
        'End Sub

        'Public Sub New(ByVal selectType As Type)
        '    _selectSrc = New ObjectSource(selectType)
        'End Sub

        'Public Sub New(ByVal entityName As String)
        '    _selectSrc = New ObjectSource(entityName)
        'End Sub

        Public Sub New(ByVal obj As IKeyEntity)
            _m2mObject = obj
        End Sub

        Public Sub New(ByVal obj As IKeyEntity, ByVal key As String)
            _m2mObject = obj
            _m2mKey = key
        End Sub

        'Public Sub New(ByVal table As SourceFragment, ByVal getMgr As ICreateManager)
        '    _from = New FromClause(table)
        '    _getMgr = getMgr
        'End Sub

        'Public Sub New(ByVal table As SourceFragment, ByVal getMgr As CreateManagerDelegate)
        '    _from = New FromClause(table)
        '    _getMgr = New CreateManager(getMgr)
        'End Sub

        'Public Sub New(ByVal selectType As Type, ByVal getMgr As ICreateManager)
        '    _selectSrc = New ObjectSource(selectType)
        '    _getMgr = getMgr
        'End Sub

        'Public Sub New(ByVal selectType As Type, ByVal getMgr As CreateManagerDelegate)
        '    _selectSrc = New ObjectSource(selectType)
        '    _getMgr = New CreateManager(getMgr)
        'End Sub

        'Public Sub New(ByVal entityName As String, ByVal getMgr As ICreateManager)
        '    _selectSrc = New ObjectSource(entityName)
        '    _getMgr = getMgr
        'End Sub

        'Public Sub New(ByVal entityName As String, ByVal getMgr As CreateManagerDelegate)
        '    _selectSrc = New ObjectSource(entityName)
        '    _getMgr = New CreateManager(getMgr)
        'End Sub

        'Public Sub New(ByVal [alias] As ObjectAlias, ByVal getMgr As CreateManagerDelegate)
        '    _selectSrc = New ObjectSource([alias])
        '    _getMgr = New CreateManager(getMgr)
        'End Sub

        'Public Sub New(ByVal [alias] As ObjectAlias, ByVal getMgr As ICreateManager)
        '    _selectSrc = New ObjectSource([alias])
        '    _getMgr = getMgr
        'End Sub

        Public Sub New(ByVal obj As IKeyEntity, ByVal getMgr As ICreateManager)
            _m2mObject = obj
            _getMgr = getMgr
        End Sub

        Public Sub New(ByVal obj As IKeyEntity, ByVal getMgr As CreateManagerDelegate)
            _m2mObject = obj
            _getMgr = New CreateManager(getMgr)
        End Sub

        Public Sub New(ByVal obj As IKeyEntity, ByVal key As String, ByVal getMgr As ICreateManager)
            _m2mObject = obj
            _m2mKey = key
            _getMgr = getMgr
        End Sub

        Public Sub New(ByVal obj As IKeyEntity, ByVal key As String, ByVal getMgr As CreateManagerDelegate)
            _m2mObject = obj
            _m2mKey = key
            _getMgr = New CreateManager(getMgr)
        End Sub
#End Region

        Protected Sub RenewMark()
            _mark = Guid.NewGuid 'Environment.TickCount
            _dic = Nothing
        End Sub

        Protected Sub RenewStatementMark()
            _statementMark = Guid.NewGuid 'Environment.TickCount
        End Sub

        Public Function Prepare(ByVal js As List(Of List(Of Worm.Criteria.Joins.QueryJoin)), _
            ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, _
            ByVal cs As List(Of List(Of SelectExpression)), ByVal stmt As StmtGenerator) As IFilter()

            Dim fs As New List(Of IFilter)
            For Each q As QueryCmd In New QueryIterator(Me)
                Dim j As New List(Of Worm.Criteria.Joins.QueryJoin)
                Dim c As List(Of SelectExpression) = Nothing
                Dim f As IFilter = q.Prepare(j, schema, filterInfo, c, stmt)
                If f IsNot Nothing Then
                    fs.Add(f)
                End If

                js.Add(j)
                cs.Add(c)
            Next

            Return fs.ToArray
        End Function

        Public Function Prepare(ByVal j As List(Of Worm.Criteria.Joins.QueryJoin), _
            ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, _
            ByRef cl As List(Of SelectExpression), ByVal stmt As StmtGenerator) As IFilter

            If propJoins IsNot Nothing Then
                j.AddRange(propJoins)
            End If

            Dim f As IFilter = Nothing
            If Filter IsNot Nothing Then
                'f = Filter.Filter(selectType)
                f = Filter.Filter()
            End If

            'For Each s As Sort In New Sort.Iterator(_order)
            '    'If s.Type Is Nothing AndAlso s.Table Is Nothing Then
            '    '    s.Type = selectType
            '    'End If
            '    If s.ObjectSource IsNot Nothing Then
            '        If s.ObjectSource.GetRealType(schema) Is Nothing AndAlso s.Table Is Nothing Then
            '            s.ObjectSource = New ObjectSource(selectType)
            '        End If

            '        If s.ObjectSource.GetRealType(schema, Nothing) IsNot Nothing AndAlso s.PropertyAlias Is Nothing AndAlso s.Column IsNot Nothing Then
            '            s.PropertyAlias = s.Column
            '            s.Column = Nothing
            '        End If
            '    End If
            'Next

            Dim selectOS As ObjectSource = GetRealSelectedOS()

            If selectOS IsNot Nothing Then
                Dim selectType As Type = selectOS.GetRealType(schema)

                If AutoJoins OrElse _m2mObject IsNot Nothing Then
                    Dim joins() As Worm.Criteria.Joins.QueryJoin = Nothing
                    Dim appendMain As Boolean
                    If OrmManager.HasJoins(schema, selectType, f, propSort, filterInfo, joins, appendMain) Then
                        j.AddRange(joins)
                    End If
                    _appendMain = appendMain
                End If

                If _m2mObject IsNot Nothing Then
                    Dim selectedType As Type = selectType
                    Dim filteredType As Type = _m2mObject.GetType

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
                        Dim en As String = schema.GetEntityNameByType(filteredType)
                        If String.IsNullOrEmpty(en) Then
                            Throw New ObjectMappingException(String.Format("Type {0} has no relation to {1}", filteredType.Name, selectedType.Name))
                        End If

                        filtered_r = schema.GetM2MRelation(selectedType, schema.GetTypeByEntityName(en), _m2mKey)

                        If filtered_r Is Nothing Then
                            Throw New ObjectMappingException(String.Format("Type {0} has no relation to {1}", filteredType.Name, selectedType.Name))
                        End If
                    End If

                    'Dim table As SourceFragment = selected_r.Table
                    Dim table As SourceFragment = filtered_r.Table

                    If table Is Nothing Then
                        Throw New ArgumentException("Invalid relation", filteredType.ToString)
                    End If

                    'Dim table As OrmTable = _o.M2M.GetTable(t, _key)

                    If _appendMain OrElse WithLoad(selectOS) OrElse IsFTS Then
                        Dim jf As New JoinFilter(table, selected_r.Column, _
                            selectType, schema.GetPrimaryKeys(selectedType)(0).PropertyAlias, Criteria.FilterOperation.Equal)
                        Dim jn As New QueryJoin(table, JoinType.Join, jf)
                        j.Add(jn)
                        If _from IsNot Nothing AndAlso table.Equals(_from.Table) Then
                            _from = Nothing
                        End If
                        If WithLoad(selectOS) Then

                        End If
                    Else
                        _from = New FromClause(table)
                        Dim r As New List(Of SelectExpression)
                        'Dim os As IOrmObjectSchemaBase = schema.GetObjectSchema(selectedType)
                        'os.GetFieldColumnMap()("ID")._columnName
                        Dim pk As ColumnAttribute = schema.GetPrimaryKeys(selectType)(0)
                        r.Add(New SelectExpression(table, selected_r.Column & " " & schema.GetColumnNameByPropertyAlias(selectedType, pk.PropertyAlias), pk.PropertyAlias))
                        r(0).Attributes = Field2DbRelations.PK
                        '_fields = New ObjectModel.ReadOnlyCollection(Of OrmProperty)(r)
                        cl = r
                    End If

                    Dim tf As New TableFilter(table, filtered_r.Column, _
                        New Worm.Criteria.Values.ScalarValue(_m2mObject.Identifier), Criteria.FilterOperation.Equal)
                    Dim con As Condition.ConditionConstructor = New Condition.ConditionConstructor
                    con.AddFilter(f)
                    con.AddFilter(tf)
                    f = con.Condition
                End If

            End If

            If _m2mObject Is Nothing Then
                If _fields IsNot Nothing Then
                    If selectOS IsNot Nothing Then
                        Dim selectType As Type = selectOS.GetRealType(schema)
                        If _autoFields Then
                            For Each pk As ColumnAttribute In schema.GetPrimaryKeys(selectType)
                                Dim find As Boolean
                                For Each fld As SelectExpression In _fields
                                    If (fld.Attributes And Field2DbRelations.PK) = Field2DbRelations.PK _
                                        AndAlso fld.PropertyAlias = pk.PropertyAlias Then
                                        find = True
                                        Exit For
                                    End If
                                Next
                                If Not find Then
                                    If cl Is Nothing Then
                                        cl = New List(Of SelectExpression)
                                    End If
                                    Dim se As New SelectExpression(selectType, pk.PropertyAlias)
                                    se.Attributes = pk._behavior
                                    cl.Add(se)
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
                Else
                    If _types IsNot Nothing Then
                        If IsFTS Then
                            cl = New List(Of SelectExpression)
                            For Each tp As Pair(Of ObjectSource, Boolean) In _types
                                If tp.Second Then
                                    Throw New NotImplementedException
                                Else
                                    Dim pk As String = schema.GetPrimaryKeys(tp.First.GetRealType(schema))(0).PropertyAlias
                                    Dim se As New SelectExpression(_from.Table, stmt.FTSKey, pk)
                                    se.Into = tp.First
                                    se.Attributes = Field2DbRelations.PK
                                    cl.Add(se)
                                End If
                            Next
                        ElseIf _from IsNot Nothing AndAlso _from.ObjectSource Is Nothing Then
                            Throw New NotSupportedException
                        Else
                            cl = New List(Of SelectExpression)
                            For Each tp As Pair(Of ObjectSource, Boolean) In _types
                                If tp.Second Then
                                    Throw New NotImplementedException
                                Else
                                    Dim pk As String = schema.GetPrimaryKeys(tp.First.GetRealType(schema))(0).PropertyAlias
                                    Dim se As New SelectExpression(tp.First, pk)
                                    se.Attributes = Field2DbRelations.PK
                                    cl.Add(se)
                                End If
                            Next
                        End If
                    Else
                        Throw New NotImplementedException
                    End If
                End If
            End If

            If _aggregates IsNot Nothing Then
                For Each a As AggregateBase In _aggregates
                    cl.Add(New SelectExpression(a))
                Next
            End If
            Return f
        End Function

        Public Function GetStaticKey(ByVal mgrKey As String, ByVal js As List(Of List(Of QueryJoin)), _
            ByVal fs() As IFilter, ByVal cb As Cache.CacheListBehavior, _
            ByVal sl As List(Of List(Of SelectExpression)), ByVal realTypeKey As String, _
            ByVal mpe As ObjectMappingEngine, ByRef dic As IDictionary) As String
            Dim key As New StringBuilder

            Dim ca As CacheDictionaryRequiredEventArgs = Nothing
            Dim cb_ As Cache.CacheListBehavior = cb

            Dim i As Integer = 0
            For Each q As QueryCmd In New QueryIterator(Me)
                If i > 0 Then
                    key.Append("$inner:")
                End If

                Dim f As IFilter = Nothing
                If fs.Length > i Then
                    f = fs(i)
                End If

                'Dim rt As Type = q.SelectedType
                'If rt Is Nothing AndAlso Not String.IsNullOrEmpty(q.SelectedEntityName) Then
                '    rt = mpe.GetTypeByEntityName(q.SelectedEntityName)
                'End If

                If Not q.GetStaticKey(key, js(i), f, cb_, sl(i), mpe) Then
                    If ca Is Nothing Then
                        ca = New CacheDictionaryRequiredEventArgs
                        RaiseEvent CacheDictionaryRequired(Me, ca)
                        If ca.GetDictionary Is Nothing Then
                            If cb = Cache.CacheListBehavior.CacheOrThrowException Then
                                Throw New QueryCmdException("Cannot cache query", Me)
                            Else
                                Return Nothing
                            End If
                        End If
                    End If
                    q.GetStaticKey(key, js(i), f, Cache.CacheListBehavior.CacheAll, sl(i), mpe)
                    cb_ = Cache.CacheListBehavior.CacheAll
                End If
                i += 1
            Next

            key.Append(realTypeKey).Append("$")

            key.Append("$").Append(mgrKey)

            If ca IsNot Nothing Then
                dic = ca.GetDictionary(key.ToString)
                Return Nothing
            Else
                Return key.ToString
            End If
        End Function

        Protected Friend Function GetStaticKey(ByVal sb As StringBuilder, ByVal j As IEnumerable(Of QueryJoin), _
            ByVal f As IFilter, ByVal cb As Cache.CacheListBehavior, _
            ByVal sl As List(Of SelectExpression), ByVal mpe As ObjectMappingEngine) As Boolean
            Dim sb2 As New StringBuilder

            If f IsNot Nothing Then
                Select Case cb
                    Case Cache.CacheListBehavior.CacheAll
                        sb2.Append(f.GetStaticString(mpe)).Append("$")
                        'Case Cache.CacheListBehavior.CacheOrThrowException
                        '    If TryCast(f, IEntityFilter) IsNot Nothing Then
                        '        sb2.Append(f.GetStaticString(mpe)).Append("$")
                        '    Else
                        '        For Each fl As IFilter In f.GetAllFilters
                        '            Dim dp As Cache.IDependentTypes = Cache.QueryDependentTypes(mpe, fl)
                        '            If Not Cache.IsCalculated(dp) Then
                        '                Throw New ApplicationException
                        '            End If
                        '        Next
                        '    End If
                    Case Cache.CacheListBehavior.CacheWhatCan, Cache.CacheListBehavior.CacheOrThrowException
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
                For Each join As QueryJoin In j
                    If Not QueryJoin.IsEmpty(join) Then
                        If join.Table IsNot Nothing Then
                            Select Case cb
                                Case Cache.CacheListBehavior.CacheAll
                                    'do nothing
                                    'Case Cache.CacheListBehavior.CacheOrThrowException
                                Case Cache.CacheListBehavior.CacheWhatCan, Cache.CacheListBehavior.CacheOrThrowException
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
                Dim rt As ICollection(Of Type) = Nothing
                If GetSelectedTypes(mpe, rt) Then
                    For Each t As Type In rt
                        sb2.Append(t.ToString)
                    Next
                ElseIf _from IsNot Nothing Then
                    If _from.Table IsNot Nothing Then
                        Select Case cb
                            Case Cache.CacheListBehavior.CacheAll
                                sb2.Append(_from.Table.RawName)
                                'Case Cache.CacheListBehavior.CacheOrThrowException
                            Case Cache.CacheListBehavior.CacheWhatCan, Cache.CacheListBehavior.CacheOrThrowException
                                Return False
                            Case Else
                                Throw New NotSupportedException(String.Format("Cache behavior {0} is not supported", cb.ToString))
                        End Select
                    Else
                    End If
                Else
                    Throw New NotSupportedException
                End If

                sb2.Append("$")
            ElseIf cb <> Cache.CacheListBehavior.CacheAll Then
                If _from IsNot Nothing Then
                    Select Case cb
                        'Case Cache.CacheListBehavior.CacheOrThrowException
                        Case Cache.CacheListBehavior.CacheWhatCan, Cache.CacheListBehavior.CacheOrThrowException
                            Return False
                        Case Else
                            Throw New NotSupportedException(String.Format("Cache behavior {0} is not supported", cb.ToString))
                    End Select
                End If
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
                If CacheSort OrElse _top IsNot Nothing OrElse cb <> Cache.CacheListBehavior.CacheAll Then
                    For Each n As Sort In New Sort.Iterator(_order)
                        If Not GetStaticKeyFromProp(sb, cb, n, mpe) Then
                            Return False
                        End If
                        sb.Append(n._ToString)
                    Next
                    sb.Append("$")
                End If
            End If

            Return True
        End Function

        Private Shared Function GetStaticKeyFromProp(ByVal sb As StringBuilder, ByVal cb As Cache.CacheListBehavior, ByVal c As SelectExpression, ByVal mpe As ObjectMappingEngine) As Boolean
            If c.IsCustom OrElse c.Query IsNot Nothing Then
                Dim dp As Cache.IDependentTypes = Cache.QueryDependentTypes(mpe, c)
                If Not Cache.IsCalculated(dp) Then
                    Select Case cb
                        Case Cache.CacheListBehavior.CacheAll
                            'do nothing
                            'Case Cache.CacheListBehavior.CacheOrThrowException
                        Case Cache.CacheListBehavior.CacheWhatCan, Cache.CacheListBehavior.CacheOrThrowException
                            Return False
                        Case Else
                            Throw New NotSupportedException(String.Format("Cache behavior {0} is not supported", cb.ToString))
                    End Select
                End If
            End If
            sb.Append(c._ToString)
            Return True
        End Function

        Public Function GetDynamicKey(ByVal js As List(Of List(Of QueryJoin)), ByVal fs() As IFilter) As String
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

        Protected Friend Sub GetDynamicKey(ByVal sb As StringBuilder, ByVal j As IEnumerable(Of QueryJoin), ByVal f As IFilter)
            If f IsNot Nothing Then
                sb.Append(f._ToString).Append("$")
            End If

            If j IsNot Nothing Then
                For Each join As QueryJoin In j
                    If Not QueryJoin.IsEmpty(join) Then
                        sb.Append(join._ToString)
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
            GetStaticKey(sb, _joins, _filter.Filter, Cache.CacheListBehavior.CacheAll, l, mpe)
            Return sb.ToString
        End Function

        Public Function GetSelectedType(ByVal mpe As ObjectMappingEngine) As Type
            Dim os As ObjectSource = GetSelectedOS()
            If os IsNot Nothing Then
                Return os.GetRealType(mpe)
            Else
                Return Nothing
            End If
        End Function

        Protected Function GetRealSelectedOS() As ObjectSource
            If _from IsNot Nothing Then
                If _from.ObjectSource IsNot Nothing Then
                    Return _from.ObjectSource
                End If
            Else
                If _types IsNot Nothing AndAlso _types.Count > 0 Then
                    Return _types(0).First
                End If
            End If
            Return Nothing
        End Function

        Public Function GetSelectedOS() As ObjectSource
            Dim os As ObjectSource = GetRealSelectedOS()
            If os Is Nothing AndAlso SelectList IsNot Nothing Then
                For Each s As SelectExpression In SelectList
                    If s.ObjectSource IsNot Nothing Then
                        os = s.ObjectSource
                        Exit For
                    End If
                Next
            End If
            Return os
        End Function

        Public Function GetSelectedTypes(ByVal mpe As ObjectMappingEngine, ByRef ts As ICollection(Of Type)) As Boolean
            If _dic Is Nothing Then
                _dic = New Dictionary(Of ObjectSource, IEntitySchema)
                ts = Nothing

                If _types IsNot Nothing Then
                    For Each p As Pair(Of ObjectSource, Boolean) In _types
                        Dim os As ObjectSource = p.First
                        If Not _dic.ContainsKey(os) Then
                            _dic.Add(os, Nothing)
                        End If
                    Next
                Else
                    For Each s As SelectExpression In SelectList
                        If s.ObjectProperty.ObjectSource Is Nothing Then
                            _dic.Clear()
                            Exit For
                        Else
                            Dim t As Type = s.ObjectProperty.ObjectSource.GetRealType(mpe)
                            If t Is Nothing OrElse GetType(AnonymousEntity).IsAssignableFrom(t) Then
                                _dic.Clear()
                                Exit For
                            ElseIf Not _dic.ContainsKey(s.ObjectProperty.ObjectSource) Then
                                _dic.Add(s.ObjectProperty.ObjectSource, Nothing)
                            End If
                        End If
                    Next
                End If
            End If
            Dim l As New List(Of Type)
            For Each os As ObjectSource In _dic.Keys
                Dim t As Type = os.GetRealType(mpe)
                If Not l.Contains(t) Then
                    l.Add(t)
                End If
            Next
            ts = l
            'ts = Array.ConvertAll(Array.a _dic
            Return l.Count > 0

        End Function

        Protected Function WithLoad(ByVal os As ObjectSource) As Boolean
            If _from IsNot Nothing Then
                Return _from.ObjectSource.Equals(os)
            Else
                For Each tp As Pair(Of ObjectSource, Boolean) In _types
                    If tp.First.Equals(os) Then
                        Return tp.Second
                    End If
                Next
            End If
            Throw New InvalidOperationException
        End Function

#Region " Properties "
        Public ReadOnly Property IsFTS() As Boolean
            Get
                If _from Is Nothing OrElse _from.Table Is Nothing Then
                    Return False
                End If

                Return GetType(SearchFragment).IsAssignableFrom(_from.Table.GetType)
            End Get
        End Property

        'Public Property SelectedEntityName() As String
        '    Get
        '        If _selectSrc Is Nothing Then
        '            Return Nothing
        '        End If
        '        Return _selectSrc.AnyEntityName
        '    End Get
        '    Set(ByVal value As String)
        '        If _selectSrc IsNot Nothing AndAlso _execCnt > 0 Then
        '            Throw New QueryCmdException("Cannot change SelectedType", Me)
        '        End If
        '        _selectSrc = New ObjectSource(value)
        '    End Set
        'End Property

        Public Property FromClaus() As FromClause
            Get
                Return _from
            End Get
            Set(ByVal value As FromClause)
                _from = value
                RenewMark()
            End Set
        End Property

        Public ReadOnly Property HasInnerQuery() As Boolean
            Get
                Return _from IsNot Nothing AndAlso _from.Query IsNot Nothing
            End Get
        End Property
        'Public Property OuterQuery() As QueryCmd
        '    Get
        '        Return _outer
        '    End Get
        '    Set(ByVal value As QueryCmd)
        '        _outer = value
        '        RenewMark()
        '    End Set
        'End Property

        Public Const RowNumerColumn As String = "[worm.row_number]"

        Public Property RowNumberFilter() As TableFilter
            Get
                Return _rn
            End Get
            Set(ByVal value As TableFilter)
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

        'Public Overridable Property SelectedType() As Type
        '    Get
        '        If _selectSrc Is Nothing Then
        '            Return Nothing
        '        End If
        '        Return _selectSrc.AnyType
        '    End Get
        '    Set(ByVal value As Type)
        '        If _selectSrc IsNot Nothing AndAlso _execCnt > 0 Then
        '            Throw New QueryCmdException("Cannot change SelectedType", Me)
        '        End If
        '        If value Is Nothing Then
        '            _selectSrc = Nothing
        '        Else
        '            _selectSrc = New ObjectSource(value)
        '        End If
        '    End Set
        'End Property

        'Public ReadOnly Property [Alias]() As ObjectAlias
        '    Get
        '        If _selectSrc Is Nothing Then
        '            Return Nothing
        '        End If
        '        Return _selectSrc.ObjectAlias
        '    End Get
        'End Property

        'Protected Friend ReadOnly Property Src() As ObjectSource
        '    Get
        '        Return _selectSrc
        '    End Get
        'End Property

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
                If _from Is Nothing Then
                    Return Nothing
                End If
                Return _from.Table
            End Get
            'Protected Friend Set(ByVal value As SourceFragment)
            '    _from = New FromClause(value)
            '    RenewMark()
            'End Set
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

        Public Property propJoins() As QueryJoin()
            Get
                Return _joins
            End Get
            Set(ByVal value As QueryJoin())
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

        Protected Property SelectTypes() As ObjectModel.ReadOnlyCollection(Of Pair(Of ObjectSource, Boolean))
            Get
                Return _types
            End Get
            Set(ByVal value As ObjectModel.ReadOnlyCollection(Of Pair(Of ObjectSource, Boolean)))
                _types = value
                _fields = Nothing
                RenewMark()
            End Set
        End Property

        Public Property SelectList() As ObjectModel.ReadOnlyCollection(Of SelectExpression)
            Get
                Return _fields
            End Get
            Set(ByVal value As ObjectModel.ReadOnlyCollection(Of SelectExpression))
                _fields = value
                _types = Nothing
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

        Public ReadOnly Property propWithLoad() As Boolean
            Get

            End Get
        End Property

        'Public Property propWithLoad() As Boolean
        '    Get
        '        Return _load
        '    End Get
        '    Set(ByVal value As Boolean)
        '        _load = value
        '        If _m2mObject Is Nothing Then
        '            RenewStatementMark()
        '        Else
        '            RenewMark()
        '        End If
        '    End Set
        'End Property
#End Region

        Public Function JoinAdd(ByVal joins() As QueryJoin) As QueryCmd
            Dim l As New List(Of QueryJoin)
            If Me.propJoins IsNot Nothing Then
                l.AddRange(Me.propJoins)
            End If
            If joins IsNot Nothing Then
                l.AddRange(joins)
            End If
            Me.propJoins = l.ToArray
            Return Me
        End Function

        Public Function Join(ByVal joins() As QueryJoin) As QueryCmd
            Me.propJoins = joins
            Return Me
        End Function

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

        'Public Function WithLoad(ByVal value As Boolean) As QueryCmd
        '    propWithLoad = value
        '    Return Me
        'End Function

        Public Function Where(ByVal filter As IGetFilter) As QueryCmd
            Me.Filter = filter
            Return Me
        End Function

        Public Function Into(ByVal t As Type) As QueryCmd
            _createType = New ObjectSource(t)
            Return Me
        End Function

        Public Function Into(ByVal entityName As String) As QueryCmd
            _createType = New ObjectSource(entityName)
            Return Me
        End Function

        Public Function From(ByVal t As Type) As QueryCmd
            _from = New FromClause(t)
            RenewMark()
            Return Me
        End Function

        Public Function From(ByVal [alias] As ObjectAlias) As QueryCmd
            _from = New FromClause([alias])
            RenewMark()
            Return Me
        End Function

        Public Function From(ByVal entityName As String) As QueryCmd
            _from = New FromClause(entityName)
            RenewMark()
            Return Me
        End Function

        Public Function From(ByVal t As SourceFragment) As QueryCmd
            _from = New FromClause(t)
            RenewMark()
            Return Me
        End Function

        Public Function From(ByVal q As QueryCmd) As QueryCmd
            _from = New FromClause(q)
            RenewMark()
            Return Me
        End Function

        Public Function [Select](ByVal ParamArray t() As Type) As QueryCmd
            SelectTypes = New ObjectModel.ReadOnlyCollection(Of Pair(Of ObjectSource, Boolean))( _
                Array.ConvertAll(Of Type, Pair(Of ObjectSource, Boolean))(t, _
                    Function(item As Type) New Pair(Of ObjectSource, Boolean)(New ObjectSource(item), False)))
            Return Me
        End Function

        Public Function [Select](ByVal ParamArray entityNames() As String) As QueryCmd
            SelectTypes = New ObjectModel.ReadOnlyCollection(Of Pair(Of ObjectSource, Boolean))( _
                Array.ConvertAll(Of String, Pair(Of ObjectSource, Boolean))(entityNames, _
                    Function(item As String) New Pair(Of ObjectSource, Boolean)(New ObjectSource(item), False)))
            Return Me
        End Function

        Public Function [Select](ByVal ParamArray aliases() As ObjectAlias) As QueryCmd
            SelectTypes = New ObjectModel.ReadOnlyCollection(Of Pair(Of ObjectSource, Boolean))( _
                Array.ConvertAll(Of ObjectAlias, Pair(Of ObjectSource, Boolean))(aliases, _
                    Function(item As ObjectAlias) New Pair(Of ObjectSource, Boolean)(New ObjectSource(item), False)))
            Return Me
        End Function

        Public Function [Select](ByVal fields() As SelectExpression) As QueryCmd
            SelectList = New ObjectModel.ReadOnlyCollection(Of SelectExpression)(fields)
            Return Me
        End Function

        Public Function [Select](ByVal t As Type, ByVal withLoad As Boolean) As QueryCmd
            Dim l As New List(Of Pair(Of ObjectSource, Boolean))(New Pair(Of ObjectSource, Boolean)() {New Pair(Of ObjectSource, Boolean)(New ObjectSource(t), withLoad)})
            SelectTypes = New ObjectModel.ReadOnlyCollection(Of Pair(Of ObjectSource, Boolean))(l)
            Return Me
        End Function

        Public Function [Select](ByVal entityName As String, ByVal withLoad As Boolean) As QueryCmd
            Dim l As New List(Of Pair(Of ObjectSource, Boolean))(New Pair(Of ObjectSource, Boolean)() {New Pair(Of ObjectSource, Boolean)(New ObjectSource(entityName), withLoad)})
            SelectTypes = New ObjectModel.ReadOnlyCollection(Of Pair(Of ObjectSource, Boolean))(l)
            Return Me
        End Function

        Public Function [Select](ByVal [alias] As ObjectAlias, ByVal withLoad As Boolean) As QueryCmd
            Dim l As New List(Of Pair(Of ObjectSource, Boolean))(New Pair(Of ObjectSource, Boolean)() {New Pair(Of ObjectSource, Boolean)(New ObjectSource([alias]), withLoad)})
            SelectTypes = New ObjectModel.ReadOnlyCollection(Of Pair(Of ObjectSource, Boolean))(l)
            Return Me
        End Function

        Public Function [SelectAdd](ByVal t As Type, ByVal withLoad As Boolean) As QueryCmd
            Dim l As New List(Of Pair(Of ObjectSource, Boolean))(SelectTypes)
            l.AddRange(New Pair(Of ObjectSource, Boolean)() {New Pair(Of ObjectSource, Boolean)(New ObjectSource(t), withLoad)})
            SelectTypes = New ObjectModel.ReadOnlyCollection(Of Pair(Of ObjectSource, Boolean))(l)
            Return Me
        End Function

        Public Function [SelectAdd](ByVal entityName As String, ByVal withLoad As Boolean) As QueryCmd
            Dim l As New List(Of Pair(Of ObjectSource, Boolean))(SelectTypes)
            l.AddRange(New Pair(Of ObjectSource, Boolean)() {New Pair(Of ObjectSource, Boolean)(New ObjectSource(entityName), withLoad)})
            SelectTypes = New ObjectModel.ReadOnlyCollection(Of Pair(Of ObjectSource, Boolean))(l)
            Return Me
        End Function

        Public Function [SelectAdd](ByVal [alias] As ObjectAlias, ByVal withLoad As Boolean) As QueryCmd
            Dim l As New List(Of Pair(Of ObjectSource, Boolean))(SelectTypes)
            l.AddRange(New Pair(Of ObjectSource, Boolean)() {New Pair(Of ObjectSource, Boolean)(New ObjectSource([alias]), withLoad)})
            SelectTypes = New ObjectModel.ReadOnlyCollection(Of Pair(Of ObjectSource, Boolean))(l)
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

        'Public Function SelectEntityName(ByVal entityName As String) As QueryCmd
        '    SelectedEntityName = entityName
        '    Return Me
        'End Function

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

#Region " ToLists "

        Public Function ToMatrix() As ReadonlyMatrix
            Throw New NotImplementedException
        End Function

#Region " ToList "

        'Public Function ToList(Of T As {_ICachedEntity})(ByVal mgr As OrmManager) As IList(Of T)
        '    Return ToEntityList(Of T)(mgr)
        'End Function

        'Public Function ToList(Of SelectType As {_ICachedEntity, New}, ReturnType As {_ICachedEntity})(ByVal mgr As OrmManager) As IList(Of ReturnType)
        '    Return GetExecutor(mgr).Exec(Of SelectType, ReturnType)(mgr, Me)
        'End Function

        Public Function ToList(ByVal mgr As OrmManager) As IList
            Dim t As MethodInfo = Me.GetType.GetMethod("ToEntityList", New Type() {GetType(OrmManager)})
            Dim st As Type = GetSelectedType(mgr.MappingEngine)
            'If st Is Nothing AndAlso Not String.IsNullOrEmpty(SelectedEntityName) Then
            '    st = mgr.MappingEngine.GetTypeByEntityName(SelectedEntityName)
            'End If
            t = t.MakeGenericMethod(New Type() {st})
            Return CType(t.Invoke(Me, New Object() {mgr}), System.Collections.IList)
        End Function

        Public Function ToList(ByVal getMgr As ICreateManager) As IList
            Using mgr As OrmManager = getMgr.CreateManager
                'mgr.RaiseObjectCreation = True
                AddHandler mgr.ObjectLoaded, AddressOf New cls(getMgr).ObjectCreated
                Return ToList(mgr)
            End Using
        End Function

        Public Function ToList() As IList
            If _getMgr Is Nothing Then
                Throw New InvalidOperationException("OrmManager required")
            End If

            Return ToList(_getMgr)
        End Function

        Public Function ToList(Of CreateType As {New, _ICachedEntity}, ReturnType As _ICachedEntity)(ByVal mgr As OrmManager) As ReadOnlyEntityList(Of ReturnType)
            Return GetExecutor(mgr).Exec(Of CreateType, ReturnType)(mgr, Me)
        End Function

        Public Function ToList(Of CreateType As {New, _ICachedEntity}, ReturnType As _ICachedEntity)(ByVal getMgr As ICreateManager) As ReadOnlyEntityList(Of ReturnType)
            Using mgr As OrmManager = getMgr.CreateManager
                'mgr.RaiseObjectCreation = True
                AddHandler mgr.ObjectLoaded, AddressOf New cls(getMgr).ObjectCreated
                Return ToList(Of CreateType, ReturnType)(mgr)
            End Using
        End Function

        Public Function ToList(Of CreateType As {New, _ICachedEntity}, ReturnType As _ICachedEntity)() As ReadOnlyEntityList(Of ReturnType)
            If _getMgr Is Nothing Then
                Throw New InvalidOperationException("OrmManager required")
            End If

            Return ToList(Of CreateType, ReturnType)(_getMgr)
        End Function

        Public Function ToList(Of CreateReturnType As {New, _ICachedEntity})(ByVal mgr As OrmManager) As ReadOnlyEntityList(Of CreateReturnType)
            Return GetExecutor(mgr).Exec(Of CreateReturnType, CreateReturnType)(mgr, Me)
        End Function

        Public Function ToList(Of CreateReturnType As {New, _ICachedEntity})(ByVal getMgr As ICreateManager) As ReadOnlyEntityList(Of CreateReturnType)
            Using mgr As OrmManager = getMgr.CreateManager
                'mgr.RaiseObjectCreation = True
                AddHandler mgr.ObjectLoaded, AddressOf New cls(getMgr).ObjectCreated
                Return ToList(Of CreateReturnType)(mgr)
            End Using
        End Function

        Public Function ToList(Of CreateReturnType As {New, _ICachedEntity})() As ReadOnlyEntityList(Of CreateReturnType)
            If _getMgr Is Nothing Then
                Throw New InvalidOperationException("OrmManager required")
            End If

            Return ToList(Of CreateReturnType)(_getMgr)
        End Function
#End Region

#Region " ToAnonymList "

        Public Function ToAnonymList(ByVal mgr As OrmManager) As ReadOnlyObjectList(Of AnonymousEntity)
            'Dim c As New svct(Me)
            '_createType = GetType(AnonymousEntity)
            'Using New OnExitScopeAction(AddressOf c.SetCT2Nothing)
            Return GetExecutor(mgr).ExecEntity(Of AnonymousEntity, AnonymousEntity)(mgr, Me)
            'End Using
        End Function

        Public Function ToAnonymList(ByVal getMgr As ICreateManager) As ReadOnlyObjectList(Of AnonymousEntity)
            Using mgr As OrmManager = getMgr.CreateManager
                'mgr.RaiseObjectCreation = True
                AddHandler mgr.ObjectLoaded, AddressOf New cls(getMgr).ObjectCreated
                Return ToAnonymList(mgr)
            End Using
        End Function

        Public Function ToAnonymList() As ReadOnlyObjectList(Of AnonymousEntity)
            If _getMgr Is Nothing Then
                Throw New InvalidOperationException("OrmManager required")
            End If

            Return ToAnonymList(_getMgr)
        End Function

#End Region

#Region " ToEntityList "

        Public Function ToEntityList(Of T As {_ICachedEntity})(ByVal getMgr As CreateManagerDelegate) As ReadOnlyEntityList(Of T)
            Dim mgr As OrmManager = getMgr()
            Try
                'mgr.RaiseObjectCreation = True
                AddHandler mgr.ObjectLoaded, AddressOf New cls(getMgr).ObjectCreated
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
                'mgr.RaiseObjectCreation = True
                AddHandler mgr.ObjectLoaded, AddressOf New cls(getMgr).ObjectCreated
                'AddHandler mgr.ObjectCreated, Function(o As ICachedEntity, m As OrmManager) AddHandler o.ManagerRequired,function(ByVal o As IEntity, ByVal args As ManagerRequiredArgs) args.Manager = getmgr)
                Return ToEntityList(Of T)(mgr)
            End Using
        End Function

        Public Function ToEntityList(Of T As {_ICachedEntity})(ByVal mgr As OrmManager) As ReadOnlyEntityList(Of T)
            If GetType(AnonymousCachedEntity).IsAssignableFrom(GetType(T)) AndAlso _createType Is Nothing Then
                Return GetExecutor(mgr).Exec(Of AnonymousCachedEntity, T)(mgr, Me)
            Else
                Return GetExecutor(mgr).Exec(Of T)(mgr, Me)
            End If
        End Function

        Public Function ToEntityList(Of T As _ICachedEntity)() As ReadOnlyEntityList(Of T)
            If _getMgr Is Nothing Then
                Throw New InvalidOperationException("OrmManager required")
            End If

            Return ToEntityList(Of T)(_getMgr)
        End Function

#End Region

#Region " ToOrmList "

        Public Function ToOrmListDyn(Of T As {_IKeyEntity})(ByVal mgr As OrmManager) As ReadOnlyList(Of T)
            Return CType(ToEntityList(Of T)(mgr), ReadOnlyList(Of T))
        End Function

        Public Function ToOrmListDyn(Of T As {_IKeyEntity})(ByVal getMgr As ICreateManager) As ReadOnlyList(Of T)
            'Using mgr As OrmManager = getMgr.CreateManager
            '    mgr.RaiseObjectCreation = True
            '    AddHandler mgr.ObjectCreated, AddressOf New cls(getMgr).ObjectCreated
            '    Return ToOrmList(Of T)(mgr)
            'End Using
            Return CType(ToEntityList(Of T)(getMgr), ReadOnlyList(Of T))
        End Function

        Public Function ToOrmListDyn(Of T As {_IKeyEntity})(ByVal getMgr As CreateManagerDelegate) As ReadOnlyList(Of T)
            Using mgr As OrmManager = getMgr()
                'mgr.RaiseObjectCreation = True
                AddHandler mgr.ObjectLoaded, AddressOf New cls(getMgr).ObjectCreated
                Return ToOrmListDyn(Of T)(mgr)
            End Using
        End Function

        Public Function ToOrmListDyn(Of T As _IKeyEntity)() As ReadOnlyList(Of T)
            If _getMgr Is Nothing Then
                Throw New InvalidOperationException("OrmManager required")
            End If

            Return ToOrmListDyn(Of T)(_getMgr)
        End Function

        Public Function ToOrmList(Of CreateType As {New, _IKeyEntity}, ReturnType As _IKeyEntity)(ByVal mgr As OrmManager) As ReadOnlyList(Of ReturnType)
            Return CType(ToList(Of CreateType, ReturnType)(mgr), ReadOnlyList(Of ReturnType))
        End Function

        Public Function ToOrmList(Of CreateType As {New, _IKeyEntity}, ReturnType As _IKeyEntity)(ByVal getMgr As ICreateManager) As ReadOnlyList(Of ReturnType)
            'Using mgr As OrmManager = getMgr.CreateManager
            '    mgr.RaiseObjectCreation = True
            '    AddHandler mgr.ObjectCreated, AddressOf New cls(getMgr).ObjectCreated
            '    Return ToOrmList(Of CreateType, ReturnType)(mgr)
            'End Using
            Return CType(ToList(Of CreateType, ReturnType)(getMgr), ReadOnlyList(Of ReturnType))
        End Function

        Public Function ToOrmList(Of CreateReturnType As {New, _IKeyEntity})(ByVal mgr As OrmManager) As ReadOnlyList(Of CreateReturnType)
            Return CType(ToList(Of CreateReturnType)(mgr), ReadOnlyList(Of CreateReturnType))
        End Function

        Public Function ToOrmList(Of CreateReturnType As {New, _IKeyEntity})(ByVal getMgr As ICreateManager) As ReadOnlyList(Of CreateReturnType)
            Return CType(ToList(Of CreateReturnType)(getMgr), ReadOnlyList(Of CreateReturnType))
        End Function

        Public Function ToOrmList(Of CreateReturnType As {New, _IKeyEntity})() As ReadOnlyList(Of CreateReturnType)
            Return CType(ToList(Of CreateReturnType)(), ReadOnlyList(Of CreateReturnType))
        End Function
#End Region

#Region " ToSimpleList "

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

        Public Function ToSimpleList(Of T)(ByVal getMgr As ICreateManager) As IList(Of T)
            Using mgr As OrmManager = getMgr.CreateManager
                'mgr.RaiseObjectCreation = True
                AddHandler mgr.ObjectLoaded, AddressOf New cls(getMgr).ObjectCreated
                Return GetExecutor(mgr).ExecSimple(Of T)(mgr, Me)
            End Using
        End Function

        Public Function ToSimpleList(Of T)() As IList(Of T)
            If _getMgr Is Nothing Then
                Throw New InvalidOperationException("OrmManager required")
            End If

            Return ToSimpleList(Of T)(_getMgr)
        End Function

        'Public Function ToSimpleList(Of CreateType As {New, _ICachedEntity}, T)(ByVal mgr As OrmManager) As IList(Of T)
        '    Return GetExecutor(mgr).ExecSimple(Of CreateType, T)(mgr, Me)
        '    'Dim q As QueryCmdBase = CreateTypedCopy(SelectedType)
        '    'Dim qt As Type = q.GetType
        '    'Dim mi As MethodInfo = qt.GetMethod("ToSimpleList")
        '    'If mi Is Nothing Then
        '    '    Throw New InvalidOperationException
        '    'End If
        '    'Dim rmi As MethodInfo = mi.MakeGenericMethod(New Type() {GetType(T)})
        '    'Return CType(rmi.Invoke(q, New Object() {mgr}), Global.System.Collections.Generic.IList(Of T))
        'End Function

        'Public Function ToSimpleList(Of CreateType As {New, _ICachedEntity}, T)(ByVal getMgr As ICreateManager) As IList(Of T)
        '    Using mgr As OrmManager = getMgr.CreateManager
        '        'mgr.RaiseObjectCreation = True
        '        AddHandler mgr.ObjectLoaded, AddressOf New cls(getMgr).ObjectCreated
        '        Return GetExecutor(mgr).ExecSimple(Of CreateType, T)(mgr, Me)
        '    End Using
        'End Function

        'Public Function ToSimpleList(Of CreateType As {New, _ICachedEntity}, T)() As IList(Of T)
        '    If _getMgr Is Nothing Then
        '        Throw New InvalidOperationException("OrmManager required")
        '    End If

        '    Return ToSimpleList(Of CreateType, T)(_getMgr)
        'End Function
#End Region

        Public Function ToObjectList(Of T As _IEntity)(ByVal mgr As OrmManager) As ReadOnlyObjectList(Of T)
            If GetType(AnonymousEntity).IsAssignableFrom(GetType(T)) AndAlso _createType Is Nothing Then
                'Dim c As New svct(Me)
                '_createType = GetType(T)
                'Using New OnExitScopeAction(AddressOf c.SetCT2Nothing)
                '    Return GetExecutor(mgr).ExecEntity(Of T)(mgr, Me)
                'End Using
                Return GetExecutor(mgr).ExecEntity(Of AnonymousEntity, T)(mgr, Me)
            Else
                Return GetExecutor(mgr).ExecEntity(Of T)(mgr, Me)
            End If
        End Function

        Public Function ToPODList(Of T As {New, Class})() As IList(Of T)
            If _getMgr Is Nothing Then
                Throw New InvalidOperationException("OrmManager required")
            End If

            Using mgr As OrmManager = _getMgr.CreateManager
                'mgr.RaiseObjectCreation = True
                AddHandler mgr.ObjectLoaded, AddressOf New cls(_getMgr).ObjectCreated
                Return ToPODList(Of T)(mgr)
            End Using
        End Function

        Public Function ToPODList(Of T As {New, Class})(ByVal mgr As OrmManager) As IList(Of T)
            Dim rt As Type = GetType(T)
            Dim mpe As ObjectMappingEngine = mgr.MappingEngine

            Dim hasPK As Boolean
            _oschema = GetSchema(mpe, rt, hasPK)

            Dim l As IEnumerable = Nothing
            Dim r As New List(Of T)

            If hasPK Then
                l = ToObjectList(Of AnonymousCachedEntity)(mgr)
            Else
                l = ToObjectList(Of AnonymousEntity)(mgr)
            End If

            Dim props As IDictionary = mpe.GetProperties(rt, _oschema)
            For Each e As _IEntity In l
                Dim ro As New T
                For Each kv As DictionaryEntry In props
                    Dim col As ColumnAttribute = CType(kv.Key, ColumnAttribute)
                    Dim pi As Reflection.PropertyInfo = CType(kv.Value, PropertyInfo)
                    Dim v As Object = mpe.GetPropertyValue(e, col.PropertyAlias, Nothing)
                    If v Is DBNull.Value Then
                        v = Nothing
                    End If
                    pi.SetValue(ro, v, Nothing)
                Next
                r.Add(ro)
            Next

            Return r
        End Function
#End Region

#Region " Singles "

#Region " Single "
        Public Function [Single](Of T As {New, _ICachedEntity})(ByVal mgr As OrmManager) As T
            Dim l As ReadOnlyEntityList(Of T) = ToList(Of T)(mgr)
            If l.Count <> 1 Then
                Throw New InvalidOperationException("Number of items is " & l.Count)
            End If
            Return l(0)
        End Function

        Public Function [Single](Of T As {New, _ICachedEntity})(ByVal getMgr As ICreateManager) As T
            Dim l As ReadOnlyEntityList(Of T) = ToList(Of T)(getMgr)
            If l.Count <> 1 Then
                Throw New InvalidOperationException("Number of items is " & l.Count)
            End If
            Return l(0)
        End Function

        Public Function [Single](Of T As {New, _ICachedEntity})() As T
            Dim l As ReadOnlyEntityList(Of T) = ToList(Of T)()
            If l.Count <> 1 Then
                Throw New InvalidOperationException("Number of items is " & l.Count)
            End If
            Return l(0)
        End Function

        Public Function [SingleOrDefault](Of T As {New, _ICachedEntity})(ByVal mgr As OrmManager) As T
            Dim l As ReadOnlyEntityList(Of T) = ToList(Of T)(mgr)
            If l.Count > 1 Then
                Throw New InvalidOperationException("Number of items is " & l.Count)
            ElseIf l.Count = 1 Then
                Return l(0)
            End If
            Return Nothing
        End Function

        Public Function SingleOrDefault(Of T As {New, _ICachedEntity})(ByVal getMgr As ICreateManager) As T
            Dim l As ReadOnlyEntityList(Of T) = ToList(Of T)(getMgr)
            If l.Count > 1 Then
                Throw New InvalidOperationException("Number of items is " & l.Count)
            ElseIf l.Count = 1 Then
                Return l(0)
            End If
            Return Nothing
        End Function

        Public Function SingleOrDefault(Of T As {New, _ICachedEntity})() As T
            Dim l As ReadOnlyEntityList(Of T) = ToList(Of T)()
            If l.Count > 1 Then
                Throw New InvalidOperationException("Number of items is " & l.Count)
            ElseIf l.Count = 1 Then
                Return l(0)
            End If
            Return Nothing
        End Function

        Public Function [SingleDyn](Of T As _ICachedEntity)(ByVal mgr As OrmManager) As T
            Dim l As ReadOnlyEntityList(Of T) = ToEntityList(Of T)(mgr)
            If l.Count <> 1 Then
                Throw New InvalidOperationException("Number of items is " & l.Count)
            End If
            Return l(0)
        End Function

        Public Function [SingleDyn](Of T As _ICachedEntity)(ByVal getMgr As ICreateManager) As T
            Dim l As ReadOnlyEntityList(Of T) = ToEntityList(Of T)(getMgr)
            If l.Count <> 1 Then
                Throw New InvalidOperationException("Number of items is " & l.Count)
            End If
            Return l(0)
        End Function

        Public Function [SingleDyn](Of T As _ICachedEntity)() As T
            Dim l As ReadOnlyEntityList(Of T) = ToEntityList(Of T)()
            If l.Count <> 1 Then
                Throw New InvalidOperationException("Number of items is " & l.Count)
            End If
            Return l(0)
        End Function

        Public Function [SingleOrDefaultDyn](Of T As _ICachedEntity)(ByVal mgr As OrmManager) As T
            Dim l As ReadOnlyEntityList(Of T) = ToEntityList(Of T)(mgr)
            If l.Count > 1 Then
                Throw New InvalidOperationException("Number of items is " & l.Count)
            ElseIf l.Count = 1 Then
                Return l(0)
            End If
            Return Nothing
        End Function

        Public Function SingleOrDefaultSyn(Of T As _ICachedEntity)(ByVal getMgr As ICreateManager) As T
            Dim l As ReadOnlyEntityList(Of T) = ToEntityList(Of T)(getMgr)
            If l.Count > 1 Then
                Throw New InvalidOperationException("Number of items is " & l.Count)
            ElseIf l.Count = 1 Then
                Return l(0)
            End If
            Return Nothing
        End Function

        Public Function SingleOrDefaultDyn(Of T As _ICachedEntity)() As T
            Dim l As ReadOnlyEntityList(Of T) = ToEntityList(Of T)()
            If l.Count > 1 Then
                Throw New InvalidOperationException("Number of items is " & l.Count)
            ElseIf l.Count = 1 Then
                Return l(0)
            End If
            Return Nothing
        End Function

#End Region

        '#Region " SingleOrm "

        '        Public Function SingleOrm(Of T As {New, _IOrmBase})(ByVal mgr As OrmManager) As T
        '            Dim l As ReadOnlyList(Of T) = ToOrmList(Of T)(mgr)
        '            If l.Count <> 1 Then
        '                Throw New InvalidOperationException("Number of items is " & l.Count)
        '            End If
        '            Return l(0)
        '        End Function

        '        Public Function SingleOrm(Of T As {New, _IOrmBase})(ByVal getMgr As ICreateManager) As T
        '            Dim l As ReadOnlyList(Of T) = ToOrmList(Of T)(getMgr)
        '            If l.Count <> 1 Then
        '                Throw New InvalidOperationException("Number of items is " & l.Count)
        '            End If
        '            Return l(0)
        '        End Function

        '        Public Function SingleOrm(Of T As {New, _IOrmBase})() As T
        '            Dim l As ReadOnlyList(Of T) = ToOrmList(Of T)()
        '            If l.Count <> 1 Then
        '                Throw New InvalidOperationException("Number of items is " & l.Count)
        '            End If
        '            Return l(0)
        '        End Function

        '        Public Function SingleOrDefaultOrm(Of T As {New, _IOrmBase})(ByVal mgr As OrmManager) As T
        '            Dim l As ReadOnlyList(Of T) = ToOrmList(Of T)(mgr)
        '            If l.Count > 1 Then
        '                Throw New InvalidOperationException("Number of items is " & l.Count)
        '            ElseIf l.Count = 1 Then
        '                Return l(0)
        '            End If
        '            Return Nothing
        '        End Function

        '        Public Function SingleOrDefaultOrm(Of T As {New, _IOrmBase})(ByVal getMgr As ICreateManager) As T
        '            Dim l As ReadOnlyList(Of T) = ToOrmList(Of T)(getMgr)
        '            If l.Count > 1 Then
        '                Throw New InvalidOperationException("Number of items is " & l.Count)
        '            ElseIf l.Count = 1 Then
        '                Return l(0)
        '            End If
        '            Return Nothing
        '        End Function

        '        Public Function SingleOrDefaultOrm(Of T As {New, _IOrmBase})() As T
        '            Dim l As ReadOnlyList(Of T) = ToOrmList(Of T)()
        '            If l.Count > 1 Then
        '                Throw New InvalidOperationException("Number of items is " & l.Count)
        '            ElseIf l.Count = 1 Then
        '                Return l(0)
        '            End If
        '            Return Nothing
        '        End Function

        '        Public Function SingleOrmDyn(Of T As _IOrmBase)(ByVal mgr As OrmManager) As T
        '            Dim l As ReadOnlyList(Of T) = ToOrmListDyn(Of T)(mgr)
        '            If l.Count <> 1 Then
        '                Throw New InvalidOperationException("Number of items is " & l.Count)
        '            End If
        '            Return l(0)
        '        End Function

        '        Public Function SingleOrmDyn(Of T As _IOrmBase)(ByVal getMgr As ICreateManager) As T
        '            Dim l As ReadOnlyList(Of T) = ToOrmListDyn(Of T)(getMgr)
        '            If l.Count <> 1 Then
        '                Throw New InvalidOperationException("Number of items is " & l.Count)
        '            End If
        '            Return l(0)
        '        End Function

        '        Public Function SingleOrmDyn(Of T As _IOrmBase)() As T
        '            Dim l As ReadOnlyList(Of T) = ToOrmListDyn(Of T)()
        '            If l.Count <> 1 Then
        '                Throw New InvalidOperationException("Number of items is " & l.Count)
        '            End If
        '            Return l(0)
        '        End Function

        '        Public Function SingleOrDefaultOrmDyn(Of T As _IOrmBase)(ByVal mgr As OrmManager) As T
        '            Dim l As ReadOnlyList(Of T) = ToOrmListDyn(Of T)(mgr)
        '            If l.Count > 1 Then
        '                Throw New InvalidOperationException("Number of items is " & l.Count)
        '            ElseIf l.Count = 1 Then
        '                Return l(0)
        '            End If
        '            Return Nothing
        '        End Function

        '        Public Function SingleOrDefaultOrmDyn(Of T As _IOrmBase)(ByVal getMgr As ICreateManager) As T
        '            Dim l As ReadOnlyList(Of T) = ToOrmListDyn(Of T)(getMgr)
        '            If l.Count > 1 Then
        '                Throw New InvalidOperationException("Number of items is " & l.Count)
        '            ElseIf l.Count = 1 Then
        '                Return l(0)
        '            End If
        '            Return Nothing
        '        End Function

        '        Public Function SingleOrDefaultOrmDyn(Of T As _IOrmBase)() As T
        '            Dim l As ReadOnlyList(Of T) = ToOrmListDyn(Of T)()
        '            If l.Count > 1 Then
        '                Throw New InvalidOperationException("Number of items is " & l.Count)
        '            ElseIf l.Count = 1 Then
        '                Return l(0)
        '            End If
        '            Return Nothing
        '        End Function

        '#End Region

#Region " SingleSimple "

        'Public Function SingleSimple(Of CreateType As {New, _ICachedEntity}, T)(ByVal mgr As OrmManager) As T
        '    Dim l As IList(Of T) = ToSimpleList(Of CreateType, T)(mgr)
        '    If l.Count <> 1 Then
        '        Throw New InvalidOperationException("Number of items is " & l.Count)
        '    End If
        '    Return l(0)
        'End Function

        'Public Function SingleSimple(Of CreateType As {New, _ICachedEntity}, T)(ByVal getMgr As ICreateManager) As T
        '    Dim l As IList(Of T) = ToSimpleList(Of CreateType, T)(getMgr)
        '    If l.Count <> 1 Then
        '        Throw New InvalidOperationException("Number of items is " & l.Count)
        '    End If
        '    Return l(0)
        'End Function

        'Public Function SingleSimple(Of CreateType As {New, _ICachedEntity}, T)() As T
        '    Dim l As IList(Of T) = ToSimpleList(Of CreateType, T)()
        '    If l.Count <> 1 Then
        '        Throw New InvalidOperationException("Number of items is " & l.Count)
        '    End If
        '    Return l(0)
        'End Function

        'Public Function [SingleOrDefaultSimple](Of CreateType As {New, _ICachedEntity}, T)(ByVal mgr As OrmManager) As T
        '    Dim l As IList(Of T) = ToSimpleList(Of CreateType, T)(mgr)
        '    If l.Count <> 1 Then
        '        Throw New InvalidOperationException("Number of items is " & l.Count)
        '    ElseIf l.Count = 1 Then
        '        Return l(0)
        '    End If
        '    Return Nothing
        'End Function

        'Public Function [SingleOrDefaultSimple](Of CreateType As {New, _ICachedEntity}, T)(ByVal getMgr As ICreateManager) As T
        '    Dim l As IList(Of T) = ToSimpleList(Of CreateType, T)(getMgr)
        '    If l.Count <> 1 Then
        '        Throw New InvalidOperationException("Number of items is " & l.Count)
        '    ElseIf l.Count = 1 Then
        '        Return l(0)
        '    End If
        '    Return Nothing
        'End Function

        'Public Function [SingleOrDefaultSimple](Of CreateType As {New, _ICachedEntity}, T)() As T
        '    Dim l As IList(Of T) = ToSimpleList(Of CreateType, T)()
        '    If l.Count <> 1 Then
        '        Throw New InvalidOperationException("Number of items is " & l.Count)
        '    ElseIf l.Count = 1 Then
        '        Return l(0)
        '    End If
        '    Return Nothing
        'End Function

        Public Function SingleSimple(Of T)(ByVal mgr As OrmManager) As T
            Dim l As IList(Of T) = ToSimpleList(Of T)(mgr)
            If l.Count <> 1 Then
                Throw New InvalidOperationException("Number of items is " & l.Count)
            End If
            Return l(0)
        End Function

        Public Function SingleSimple(Of T)(ByVal getMgr As ICreateManager) As T
            Dim l As IList(Of T) = ToSimpleList(Of T)(getMgr)
            If l.Count <> 1 Then
                Throw New InvalidOperationException("Number of items is " & l.Count)
            End If
            Return l(0)
        End Function

        Public Function SingleSimple(Of T)() As T
            Dim l As IList(Of T) = ToSimpleList(Of T)()
            If l.Count <> 1 Then
                Throw New InvalidOperationException("Number of items is " & l.Count)
            End If
            Return l(0)
        End Function

        Public Function [SingleOrDefaultSimple](Of T)(ByVal mgr As OrmManager) As T
            Dim l As IList(Of T) = ToSimpleList(Of T)(mgr)
            If l.Count <> 1 Then
                Throw New InvalidOperationException("Number of items is " & l.Count)
            ElseIf l.Count = 1 Then
                Return l(0)
            End If
            Return Nothing
        End Function

        Public Function [SingleOrDefaultSimple](Of T)(ByVal getMgr As ICreateManager) As T
            Dim l As IList(Of T) = ToSimpleList(Of T)(getMgr)
            If l.Count <> 1 Then
                Throw New InvalidOperationException("Number of items is " & l.Count)
            ElseIf l.Count = 1 Then
                Return l(0)
            End If
            Return Nothing
        End Function

        Public Function [SingleOrDefaultSimple](Of T)() As T
            Dim l As IList(Of T) = ToSimpleList(Of T)()
            If l.Count <> 1 Then
                Throw New InvalidOperationException("Number of items is " & l.Count)
            ElseIf l.Count = 1 Then
                Return l(0)
            End If
            Return Nothing
        End Function
#End Region

#End Region

        Private Function GetSchema(ByVal mpe As ObjectMappingEngine, ByVal t As Type, _
                                   ByRef pk As Boolean) As IEntitySchema
            If SelectList Is Nothing Then
                Dim selList As New OrmObjectIndex
                For Each de As DictionaryEntry In ObjectMappingEngine.GetMappedProperties(t)
                    Dim c As ColumnAttribute = CType(de.Key, ColumnAttribute)
                    selList.Add(New MapField2Column(c.PropertyAlias, c.Column, Nothing))
                Next
                Return New SimpleObjectSchema(selList)
            Else
                Return New SimpleObjectSchema(SelectExpression.GetMapping(SelectList))
            End If
        End Function

        Protected Friend Function GetSchemaForCreateType(ByVal mpe As ObjectMappingEngine) As IEntitySchema
            If CreateType IsNot Nothing Then
                Return mpe.GetObjectSchema(CreateType.GetRealType(mpe), False)
            Else
                Return _oschema
            End If
        End Function

        Public Sub Reset(Of ReturnType As _IEntity)(ByVal mgr As OrmManager)
            GetExecutor(mgr).ResetEntity(Of ReturnType)(mgr, Me)
        End Sub

        Public Sub RenewDyn(Of ReturnType As _ICachedEntity)(ByVal mgr As OrmManager)
            GetExecutor(mgr).Reset(Of ReturnType)(mgr, Me)
        End Sub

        Public Sub Renew(Of CreateType As {_ICachedEntity, New}, ReturnType As _ICachedEntity)(ByVal mgr As OrmManager)
            GetExecutor(mgr).Reset(Of CreateType, ReturnType)(mgr, Me)
        End Sub

        Public Sub Renew(Of CreateReturnType As {_ICachedEntity, New})(ByVal mgr As OrmManager)
            GetExecutor(mgr).Reset(Of CreateReturnType, CreateReturnType)(mgr, Me)
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
                '._load = _load
                ._mark = ._mark
                ._m2mObject = _m2mObject
                ._order = _order
                ._page = _page
                ._statementMark = _statementMark
                '._selectSrc = _selectSrc
                ._from = _from
                ._top = _top
                ._rn = _rn
                '._outer = _outer
                ._er = _er
                '._en = _en
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
                ._timeout = _timeout
                ._oschema = _oschema
                ._autoFields = _autoFields
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

        Private Function [Get](ByVal mpe As ObjectMappingEngine) As Cache.IDependentTypes Implements Cache.IQueryDependentTypes.Get
            'If SelectedType Is Nothing Then
            '    Return New Cache.EmptyDependentTypes
            'End If

            Dim dp As New Cache.DependentTypes
            If _joins IsNot Nothing Then
                For Each j As QueryJoin In _joins
                    If j.ObjectSource IsNot Nothing Then
                        Dim t As Type = j.ObjectSource.GetRealType(mpe)
                        'If t Is Nothing AndAlso Not String.IsNullOrEmpty(j.EntityName) Then
                        '    t = mpe.GetTypeByEntityName(j.EntityName)
                        'End If
                        'If t Is Nothing Then
                        '    Return New Cache.EmptyDependentTypes
                        'End If
                        If t IsNot Nothing Then
                            dp.AddBoth(t)
                        End If
                    End If
                Next
            End If

            Dim types As ICollection(Of Type) = Nothing
            Dim rt As Boolean = GetSelectedTypes(mpe, types)

            If rt AndAlso Not dp.IsEmpty Then
                dp.AddBoth(types)
            End If

            If _filter IsNot Nothing AndAlso TryCast(_filter, IEntityFilter) Is Nothing Then
                For Each f As IFilter In _filter.Filter.GetAllFilters
                    Dim fdp As Cache.IDependentTypes = Cache.QueryDependentTypes(mpe, f)
                    If Cache.IsCalculated(fdp) Then
                        If rt Then
                            dp.AddBoth(types)
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
                        If rt Then
                            dp.AddBoth(types)
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

        Public Function GetOrmCommand(Of T As {New, _IKeyEntity})(ByVal mgr As OrmManager) As OrmQueryCmd(Of T)
            'Dim f As ICreateQueryCmd = TryCast(mgr, ICreateQueryCmd)
            Dim q As New OrmQueryCmd(Of T)()
            CopyTo(q)
            If _getMgr Is Nothing Then
                q.Exec(mgr)
            End If
            Return q
        End Function

        Public Function GetOrmCommand(Of T As {New, _IKeyEntity})() As OrmQueryCmd(Of T)
            Dim mgr As OrmManager = OrmManager.CurrentManager
            Return GetOrmCommand(Of T)(mgr)
        End Function

#Region " Create methods "

        Public Shared Function Create(ByVal mgr As OrmManager) As QueryCmd
            Dim f As ICreateQueryCmd = TryCast(mgr, ICreateQueryCmd)
            Dim q As QueryCmd = Nothing
            If f Is Nothing Then
                q = New QueryCmd()
            Else
                q = f.Create()
            End If
            Dim cm As ICreateManager = TryCast(mgr, ICreateManager)
            If cm IsNot Nothing Then
                q._getMgr = cm
            End If
            Return q
        End Function

        Public Shared Function CreateAndGetOrmCommand(Of T As {New, _IKeyEntity})(ByVal mgr As OrmManager) As OrmQueryCmd(Of T)
            Dim selectType As Type = GetType(T)
            Return Create(mgr).GetOrmCommand(Of T)(mgr)
        End Function

        'Public Shared Function Create(ByVal selectType As Type, ByVal mgr As OrmManager) As QueryCmd
        '    Dim f As ICreateQueryCmd = TryCast(mgr, ICreateQueryCmd)
        '    Dim q As QueryCmd = Nothing
        '    If f Is Nothing Then
        '        q = New QueryCmd(selectType)
        '    Else
        '        q = f.Create(selectType)
        '    End If
        '    Dim cm As ICreateManager = TryCast(mgr, ICreateManager)
        '    If cm IsNot Nothing Then
        '        q._getMgr = cm
        '    End If
        '    Return q
        'End Function

        'Public Shared Function CreateByEntityName(ByVal entityName As String, ByVal mgr As OrmManager) As QueryCmd
        '    Dim f As ICreateQueryCmd = TryCast(mgr, ICreateQueryCmd)
        '    Dim q As QueryCmd = Nothing
        '    If f Is Nothing Then
        '        q = New QueryCmd(entityName)
        '    Else
        '        q = f.CreateByEntityName(entityName)
        '    End If
        '    Dim cm As ICreateManager = TryCast(mgr, ICreateManager)
        '    If cm IsNot Nothing Then
        '        q._getMgr = cm
        '    End If
        '    Return q
        'End Function

        Public Shared Function Create(ByVal obj As IKeyEntity, ByVal mgr As OrmManager) As QueryCmd
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

        Public Shared Function Create(ByVal obj As IKeyEntity, ByVal key As String, ByVal mgr As OrmManager) As QueryCmd
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

        'Public Shared Function Create(ByVal name As String, ByVal table As SourceFragment, ByVal mgr As OrmManager) As QueryCmd
        '    Dim f As ICreateQueryCmd = TryCast(mgr, ICreateQueryCmd)
        '    Dim q As QueryCmd = Nothing
        '    If f Is Nothing Then
        '        q = New QueryCmd(table)
        '        q.Name = name
        '    Else
        '        q = f.Create(name, table)
        '    End If
        '    Dim cm As ICreateManager = TryCast(mgr, ICreateManager)
        '    If cm IsNot Nothing Then
        '        q._getMgr = cm
        '    End If
        '    Return q
        'End Function

        Public Shared Function CreateAndGetOrmCommand(Of T As {New, _IKeyEntity})(ByVal name As String, ByVal mgr As OrmManager) As OrmQueryCmd(Of T)
            Dim selectType As Type = GetType(T)
            Return Create(name, mgr).GetOrmCommand(Of T)(mgr)
        End Function

        Public Shared Function Create(ByVal name As String, ByVal mgr As OrmManager) As QueryCmd
            Dim f As ICreateQueryCmd = TryCast(mgr, ICreateQueryCmd)
            Dim q As QueryCmd = Nothing
            If f Is Nothing Then
                q = New QueryCmd()
                q.Name = name
            Else
                q = f.Create(name)
            End If
            Dim cm As ICreateManager = TryCast(mgr, ICreateManager)
            If cm IsNot Nothing Then
                q._getMgr = cm
            End If
            Return q
        End Function

        'Public Shared Function CreateByEntityName(ByVal name As String, ByVal entityName As String, ByVal mgr As OrmManager) As QueryCmd
        '    Dim f As ICreateQueryCmd = TryCast(mgr, ICreateQueryCmd)
        '    Dim q As QueryCmd = Nothing
        '    If f Is Nothing Then
        '        q = New QueryCmd(entityName)
        '        q.Name = name
        '    Else
        '        q = f.CreateByEntityName(name, entityName)
        '    End If
        '    Dim cm As ICreateManager = TryCast(mgr, ICreateManager)
        '    If cm IsNot Nothing Then
        '        q._getMgr = cm
        '    End If
        '    Return q
        'End Function

        Public Shared Function Create(ByVal name As String, ByVal obj As IKeyEntity, ByVal mgr As OrmManager) As QueryCmd
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

        Public Shared Function Create(ByVal name As String, ByVal obj As IKeyEntity, ByVal key As String, ByVal mgr As OrmManager) As QueryCmd
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

        Public Shared Function Create() As QueryCmd
            Return Create(OrmManager.CurrentManager)
        End Function

        Public Shared Function CreateAndGetOrmCommand(Of T As {New, _IKeyEntity})() As OrmQueryCmd(Of T)
            Return CreateAndGetOrmCommand(Of T)(OrmManager.CurrentManager)
        End Function

        'Public Shared Function Create(ByVal selectType As Type) As QueryCmd
        '    Return Create(selectType, OrmManager.CurrentManager)
        'End Function

        'Public Shared Function CreateByEntityName(ByVal entityName As String) As QueryCmd
        '    Return CreateByEntityName(entityName, OrmManager.CurrentManager)
        'End Function

        Public Shared Function Create(ByVal obj As IKeyEntity) As QueryCmd
            Return Create(obj, OrmManager.CurrentManager)
        End Function

        Public Shared Function Create(ByVal obj As IKeyEntity, ByVal key As String) As QueryCmd
            Return Create(obj, key, OrmManager.CurrentManager)
        End Function

        'Public Shared Function Create(ByVal name As String, ByVal table As SourceFragment) As QueryCmd
        '    Return Create(name, table, OrmManager.CurrentManager)
        'End Function

        Public Shared Function CreateAndGetOrmCommand(Of T As {New, _IKeyEntity})(ByVal name As String) As OrmQueryCmd(Of T)
            Return CreateAndGetOrmCommand(Of T)(name, OrmManager.CurrentManager)
        End Function

        Public Shared Function Create(ByVal name As String) As QueryCmd
            Return Create(name, OrmManager.CurrentManager)
        End Function

        'Public Shared Function CreateByEntityName(ByVal name As String, ByVal entityName As String) As QueryCmd
        '    Return CreateByEntityName(name, entityName, OrmManager.CurrentManager)
        'End Function

        Public Shared Function Create(ByVal name As String, ByVal obj As IKeyEntity) As QueryCmd
            Return Create(name, obj, OrmManager.CurrentManager)
        End Function

        Public Shared Function Create(ByVal name As String, ByVal obj As IKeyEntity, ByVal key As String) As QueryCmd
            Return Create(name, obj, key, OrmManager.CurrentManager)
        End Function

        Public Shared Function Search(ByVal t As Type, ByVal searchText As String, ByVal getMgr As CreateManagerDelegate) As QueryCmd
            Dim q As New QueryCmd(New CreateManager(getMgr))
            q.From(New SearchFragment(t, searchText))
            Return q
        End Function

        Public Shared Function Search(ByVal t As Type, ByVal searchText As String, ByVal getMgr As ICreateManager) As QueryCmd
            Dim q As New QueryCmd(getMgr)
            q.From(New SearchFragment(t, searchText))
            Return q
        End Function

        Public Shared Function Search(ByVal searchText As String, ByVal getMgr As ICreateManager) As QueryCmd
            Dim q As New QueryCmd(getMgr)
            q.From(New SearchFragment(searchText))
            Return q
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

    Public Class OrmQueryCmd(Of T As {New, _IKeyEntity})
        Inherits QueryCmd
        Implements Generic.IEnumerable(Of T)

        Private _preCmp As ReadOnlyList(Of T)
        Private _oldMark As Guid

        Public Sub Exec(ByVal mgr As OrmManager)
            _preCmp = ToOrmListDyn(Of T)(mgr)
            _oldMark = _mark
        End Sub

        Public Shared Widening Operator CType(ByVal cmd As OrmQueryCmd(Of T)) As ReadOnlyList(Of T)
            If cmd._getMgr IsNot Nothing Then
                Return cmd.ToOrmList(Of T, T)(cmd._getMgr)
            ElseIf cmd._preCmp IsNot Nothing AndAlso cmd._oldMark = cmd._mark Then
                Return cmd._preCmp
            Else
                Throw New InvalidOperationException("Cannot convert to list")
            End If
        End Operator

#Region " Ctors "
        Public Sub New()
        End Sub

        'Public Sub New(ByVal table As SourceFragment)
        '    MyBase.New(table)
        'End Sub

        'Public Sub New(ByVal selectType As Type)
        '    MyBase.New(selectType)
        'End Sub

        'Public Sub New(ByVal entityName As String)
        '    MyBase.New(entityName)
        'End Sub

        Public Sub New(ByVal obj As IKeyEntity)
            MyBase.New(obj)
        End Sub

        Public Sub New(ByVal obj As IKeyEntity, ByVal key As String)
            MyBase.New(obj, key)
        End Sub

        Public Sub New(ByVal getMgr As ICreateManager)
            MyBase.New(getMgr)
        End Sub

        'Public Sub New(ByVal selectType As Type, ByVal getMgr As ICreateManager)
        '    MyBase.New(selectType, getMgr)
        'End Sub

        'Public Sub New(ByVal entityName As String, ByVal getMgr As ICreateManager)
        '    MyBase.New(entityName, getMgr)
        'End Sub

        Public Sub New(ByVal obj As _IKeyEntity, ByVal getMgr As ICreateManager)
            MyBase.New(obj, getMgr)
        End Sub

        Public Sub New(ByVal obj As _IKeyEntity, ByVal key As String, ByVal getMgr As ICreateManager)
            MyBase.New(obj, key, getMgr)
        End Sub

#End Region

        Public Function GetEnumerator() As System.Collections.Generic.IEnumerator(Of T) Implements System.Collections.Generic.IEnumerable(Of T).GetEnumerator
            Return CType(Me, ReadOnlyList(Of T)).GetEnumerator
        End Function

        Protected Function _GetEnumerator() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
            Return GetEnumerator()
        End Function

        'Public Overrides Property SelectedType() As System.Type
        '    Get
        '        If MyBase.SelectedType Is Nothing Then
        '            Return GetType(T)
        '        Else
        '            Return MyBase.SelectedType
        '        End If
        '    End Get
        '    Set(ByVal value As System.Type)
        '        MyBase.SelectedType = value
        '    End Set
        'End Property
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

    'Public Class SearchQueryCmd
    '    Inherits QueryCmd

    '    Public Sub New(ByVal t As Type, ByVal searchString As String, ByVal getMgr As ICreateManager)
    '        MyBase.New(New SearchFragment(t, searchString), getMgr)
    '    End Sub

    '    Public Sub New(ByVal searchString As String, ByVal getMgr As ICreateManager)
    '        MyBase.New(New SearchFragment(searchString), getMgr)
    '    End Sub

    '    Public Sub New(ByVal obj As _IOrmBase, ByVal searchString As String)
    '        MyBase.New(obj)
    '        _table = New SearchFragment(obj.GetType, searchString)
    '    End Sub
    'End Class
End Namespace