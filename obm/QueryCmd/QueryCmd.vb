﻿Imports System.Collections.Generic
Imports System.Collections.ObjectModel
Imports System.ComponentModel
Imports System.Reflection
Imports Worm.Cache
Imports Worm.Criteria.Conditions
Imports Worm.Criteria.Core
Imports Worm.Criteria.Joins
Imports Worm.Criteria.Values
Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Expressions2
Imports Worm.Misc
Imports Worm.Query.Sorting
Imports System.Linq

Namespace Query

    '<Serializable()> _
    Public Class QueryCmd
        Implements ICloneable, Criteria.Values.IQueryElement, IExecutionContext
        'Implements Cache.IQueryDependentTypes

#Region " Classes "

        <Serializable()> _
        Class SelectClauseDef
            Implements ICopyable

            Private _fields As ObjectModel.ReadOnlyCollection(Of SelectExpression)
            Private _types As ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))

            Protected Sub New()

            End Sub
            Public Sub New(ByVal types As ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?)))
                _types = types
            End Sub

            Public Sub New(ByVal types As List(Of Pair(Of EntityUnion, Boolean?)))
                _types = New ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))(types)
            End Sub

            Public Sub New(ByVal fields As ObjectModel.ReadOnlyCollection(Of SelectExpression))
                _fields = fields
            End Sub

            Public Sub New(ByVal fields As List(Of SelectExpression))
                _fields = New ObjectModel.ReadOnlyCollection(Of SelectExpression)(fields)
            End Sub

            Public ReadOnly Property SelectTypes() As ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))
                Get
                    Return _types
                End Get
            End Property

            Public ReadOnly Property SelectList() As ObjectModel.ReadOnlyCollection(Of SelectExpression)
                Get
                    Return _fields
                End Get
            End Property

            Protected Overridable Function _Clone() As Object Implements ICloneable.Clone
                Return Clone()
            End Function

            Public Function Clone() As SelectClauseDef
                Dim n As New SelectClauseDef()
                CopyTo(n)
                Return n
            End Function
            Protected Overridable Function _CopyTo(target As ICopyable) As Boolean Implements ICopyable.CopyTo
                Return CopyTo(TryCast(target, SelectClauseDef))
            End Function

            Public Function CopyTo(target As SelectClauseDef) As Boolean
                If target Is Nothing Then
                    Return False
                End If

                If _types IsNot Nothing Then
                    target._types = New ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))(_types)
                End If

                If _fields IsNot Nothing Then
                    target._fields = New ReadOnlyCollection(Of SelectExpression)(_fields)
                End If

                Return True
            End Function
        End Class

        <Serializable()> _
        Class FromClauseDef
            Implements ICopyable

            Public ObjectSource As EntityUnion
            Public Table As SourceFragment
            Public Query As QueryCmd
            Private _qeu As EntityUnion

            Public Sub New(ByVal table As SourceFragment, Optional hint As String = Nothing)
                If table Is Nothing Then
                    Throw New ArgumentNullException(NameOf(table))
                End If
                Me.Table = table
                Me.Hint = hint
            End Sub

            Public Sub New(ByVal query As QueryCmd)
                If query Is Nothing Then
                    Throw New ArgumentNullException(NameOf(query))
                End If
                Me.Query = query
            End Sub

            Public Sub New(ByVal [alias] As QueryAlias)
                If [alias] Is Nothing Then
                    Throw New ArgumentNullException(NameOf([alias]))
                End If
                Me.ObjectSource = New EntityUnion([alias])
            End Sub

            Public Sub New(ByVal t As Type, Optional hint As String = Nothing)
                If t Is Nothing Then
                    Throw New ArgumentNullException(NameOf(t))
                End If
                Me.ObjectSource = New EntityUnion(t)
                Me.Hint = hint
            End Sub

            Public Sub New(ByVal entityName As String, Optional hint As String = Nothing)
                If String.IsNullOrEmpty(entityName) Then
                    Throw New ArgumentNullException(NameOf(entityName))
                End If
                Me.ObjectSource = New EntityUnion(entityName)
                Me.Hint = hint
            End Sub

            Public Sub New(ByVal os As EntityUnion, Optional hint As String = Nothing)
                If os Is Nothing Then
                    Throw New ArgumentNullException(NameOf(os))
                End If
                Me.ObjectSource = os
                Me.Hint = hint
            End Sub

            Protected Sub New()

            End Sub
            Public Property Hint As String
            Public ReadOnly Property QueryEU() As EntityUnion
                Get
                    If Query IsNot Nothing Then
                        If _qeu Is Nothing Then
                            _qeu = New EntityUnion(New QueryAlias(Query))
                        End If
                        Return _qeu
                    End If
                    'If ObjectSource IsNot Nothing AndAlso ObjectSource.AnyType Is Nothing AndAlso String.IsNullOrEmpty(ObjectSource.AnyEntityName) Then
                    '    Return ObjectSource
                    'End If
                    Return ObjectSource
                End Get
            End Property

            Public Function GetFromEntity() As EntityUnion
                Dim eu As EntityUnion = QueryEU
                If eu Is Nothing AndAlso GetType(SearchFragment).IsAssignableFrom(Table.GetType) Then
                    eu = CType(Table, SearchFragment).Entity
                End If
                Return eu
            End Function

            Public ReadOnly Property AnyQuery() As QueryCmd
                Get
                    If Query IsNot Nothing Then
                        Return Query
                    ElseIf ObjectSource IsNot Nothing AndAlso ObjectSource.AnyType Is Nothing AndAlso String.IsNullOrEmpty(ObjectSource.AnyEntityName) Then
                        Return ObjectSource.ObjectAlias.Query
                    End If
                    Return Nothing
                End Get
            End Property

            Protected Overridable Function _Clone() As Object Implements ICloneable.Clone
                Return Clone()
            End Function

            Public Function Clone() As FromClauseDef
                Dim n As New FromClauseDef()
                CopyTo(n)
                Return n
            End Function

            Protected Overridable Function _CopyTo(target As ICopyable) As Boolean Implements ICopyable.CopyTo
                Return CopyTo(TryCast(target, FromClauseDef))
            End Function

            Public Function CopyTo(target As FromClauseDef) As Boolean
                If target Is Nothing Then
                    Return False
                End If

                If ObjectSource IsNot Nothing Then
                    target.ObjectSource = ObjectSource.Clone
                End If

                If Table IsNot Nothing Then
                    target.Table = Table.Clone
                End If

                If Query IsNot Nothing Then
                    target.Query = Query.Clone
                End If

                Return True
            End Function
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
            Private ReadOnly _key As String

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

        <EditorBrowsable(EditorBrowsableState.Never)> _
        Class svct
            Private ReadOnly _oldct As Dictionary(Of EntityUnion, EntityUnion)
            Protected _types As ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))
            Private ReadOnly _cmd As QueryCmd
            Private ReadOnly _f As FromClauseDef
            Private ReadOnly _sssl As ObjectModel.ReadOnlyCollection(Of SelectExpression)

            Private _dontReset As Boolean

            Public Property DontReset() As Boolean
                Get
                    Return _dontReset
                End Get
                Set(ByVal value As Boolean)
                    _dontReset = value
                End Set
            End Property

            Sub New(ByVal cmd As QueryCmd)
                _oldct = New Dictionary(Of EntityUnion, EntityUnion)(cmd._createTypes)
                If cmd.SelectClause IsNot Nothing Then
                    If cmd.SelectClause.SelectTypes IsNot Nothing Then
                        _types = New ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))(cmd.SelectClause.SelectTypes)
                    ElseIf cmd.SelectClause.SelectList IsNot Nothing Then
                        _sssl = New ObjectModel.ReadOnlyCollection(Of SelectExpression)(cmd.SelectClause.SelectList)
                    End If
                End If
                _cmd = cmd
                _f = cmd._from
            End Sub

            Public Sub SetCT2Nothing()
                If Not _dontReset Then
                    _cmd._createTypes = _oldct
                    If _types Is Nothing AndAlso _sssl Is Nothing Then
                        _cmd._sel = Nothing
                    ElseIf _types IsNot Nothing Then
                        _cmd._sel = New SelectClauseDef(_types)
#If nlog Then
                        'NLog.LogManager.GetCurrentClassLogger?.Trace("_cmd._sel types {0}", Environment.StackTrace)
#End If
                    ElseIf _sssl IsNot Nothing Then
                        _cmd._sel = New SelectClauseDef(_sssl)
#If nlog Then
                        'NLog.LogManager.GetCurrentClassLogger?.Trace("_cmd._sel selexp {0}", Environment.StackTrace)
#End If
                    Else
                        Throw New NotSupportedException
                    End If
                    _cmd._from = _f
                End If
            End Sub
        End Class

        <EditorBrowsable(EditorBrowsableState.Never)> _
        Class RowNumberFilterInfo
            Public Enum RowNumberFilterStatus
                IsClearFilter
                IsSkip
                IsTake
                IsSkipTake
            End Enum

            Protected _status As RowNumberFilterStatus
            Protected _fromPosition As Integer
            Protected _toPosition As Integer

            Public ReadOnly Property Status() As RowNumberFilterStatus
                Get
                    Return _status
                End Get
            End Property

            Public ReadOnly Property FromPostion() As Integer
                Get
                    Return _fromPosition
                End Get
            End Property

            Public ReadOnly Property ToPostion() As Integer
                Get
                    Return _fromPosition
                End Get
            End Property

            Public Sub New(ByVal filter As TableFilter)
                If Not filter Is Nothing Then
                    Dim scalarValue As ScalarValue = TryCast(filter.Value, ScalarValue)

                    If Not scalarValue Is Nothing Then
                        If filter.Template.Operation = Criteria.FilterOperation.GreaterThan Then
                            _status = RowNumberFilterStatus.IsSkip
                            _fromPosition = CType(scalarValue.Value, Int32)
                        ElseIf filter.Template.Operation = Criteria.FilterOperation.LessEqualThan Then
                            _status = RowNumberFilterStatus.IsTake
                            _toPosition = CType(scalarValue.Value, Int32)
                        End If
                    Else
                        Dim betweenValue As BetweenValue = TryCast(filter.Value, BetweenValue)

                        If Not betweenValue Is Nothing Then
                            _status = RowNumberFilterStatus.IsSkipTake
                            _fromPosition = CType(CType(betweenValue.Value.First, ScalarValue).Value, Int32)
                            _toPosition = CType(CType(betweenValue.Value.Second, ScalarValue).Value, Int32)
                        Else
                            Throw New NotSupportedException(String.Format("Filter of type {0} is not supported", filter.GetType))
                        End If
                    End If

                Else
                    _status = RowNumberFilterStatus.IsClearFilter
                End If
            End Sub
        End Class

        Public Class GetDynamicKey4FilterEventArgs
            Inherits EventArgs

            Private ReadOnly _f As IGetFilter
            Private _key As String

            Public Sub New(ByVal f As IGetFilter)
                _f = f
            End Sub

            Public ReadOnly Property Filter() As IGetFilter
                Get
                    Return _f
                End Get
            End Property

            Public Property CustomKey() As String
                Get
                    Return _key
                End Get
                Set(ByVal value As String)
                    _key = value
                End Set
            End Property
        End Class

        Public Class QueryPreparedEventArgs
            Inherits EventArgs

            Private _cancel As Boolean
            Public Property Cancel() As Boolean
                Get
                    Return _cancel
                End Get
                Set(ByVal value As Boolean)
                    _cancel = value
                End Set
            End Property
        End Class

        Public Class ModifyResultArgs

            Private _col As ICollection

            Public Property Matrix() As ReadonlyMatrix
                Get
                    Return CType(_col, ReadonlyMatrix)
                End Get
                Set(ByVal value As ReadonlyMatrix)
                    _col = value
                End Set
            End Property

            Public Property ReadOnlyList() As IReadOnlyList
                Get
                    Return CType(_col, IReadOnlyList)
                End Get
                Set(ByVal value As IReadOnlyList)
                    _col = value
                End Set
            End Property

            Public Property SimpleList() As ICollection
                Get
                    Return _col
                End Get
                Set(ByVal value As ICollection)
                    _col = value
                End Set
            End Property

            Public Shared EmptyCustomInfo As New Object
            Private _o As Object = EmptyCustomInfo
            Public Property CustomInfo() As Object
                Get
                    Return _o
                End Get
                Set(ByVal value As Object)
                    _o = value
                End Set
            End Property

            Public ReadOnly Property IsCustomInfoSet As Boolean
                Get
                    Return Not EmptyCustomInfo.Equals(_o)
                End Get
            End Property
            Private ReadOnly _executed As Boolean
            Private ReadOnly _mgr As OrmManager

            Public ReadOnly Property FromCache() As Boolean
                Get
                    Return Not _executed
                End Get
            End Property

            Public ReadOnly Property OrmManager() As OrmManager
                Get
                    Return _mgr
                End Get
            End Property

            Public ReadOnly Property IsSimple() As Boolean
                Get
                    Dim t As Type = _col.GetType
                    Return Not (GetType(IReadOnlyList).IsAssignableFrom(t) OrElse GetType(ReadonlyMatrix).IsAssignableFrom(t))
                End Get
            End Property

            Public Sub New()
            End Sub

            Public Sub New(ByVal mgr As OrmManager, ByVal fromCache As Boolean)
                _executed = Not fromCache
                _mgr = mgr
            End Sub
        End Class

        Public Class ConnectionExceptionArgs
            Inherits EventArgs

            Enum ActionEnum
                Rethrow
                RetryOldConnection
                ''' <summary>
                ''' Create new connection and retry
                ''' </summary>
                ''' <remarks>New connection string in <see cref="ConnectionExceptionArgs.Context"/> property</remarks>
                RetryNewConnection
                ''' <summary>
                ''' Rethrow custom exception
                ''' </summary>
                ''' <remarks>Custom exception in <see cref="ConnectionExceptionArgs.Context"/> property</remarks>
                RethrowCustom
            End Enum

            Property Action As ActionEnum

            Property Context As Object

            Private ReadOnly _ex As Exception
            Public ReadOnly Property Exception() As Exception
                Get
                    Return _ex
                End Get
            End Property

            Private ReadOnly _conn As Object
            Public ReadOnly Property Connection() As Object
                Get
                    Return _conn
                End Get
            End Property

            Private ReadOnly _mgr As OrmManager
            Public ReadOnly Property OrmManager() As OrmManager
                Get
                    Return _mgr
                End Get
            End Property

            Public Sub New(ex As Exception, conn As Object, mgr As OrmManager)
                _ex = ex
                _conn = conn
                _mgr = mgr
            End Sub
        End Class

        Public Class CommandExceptionArgs
            Inherits EventArgs

            Enum ActionEnum
                Rethrow
                RetryOldConnection
                ''' <summary>
                ''' Create new connection and retry
                ''' </summary>
                ''' <remarks>New connection string in <see cref="ConnectionExceptionArgs.Context"/> property</remarks>
                RetryNewConnection
                ''' <summary>
                ''' Rethrow custom exception
                ''' </summary>
                ''' <remarks>Custom exception in <see cref="ConnectionExceptionArgs.Context"/> property</remarks>
                RethrowCustom
                RetryNewCommand
                'RetryNewCommandOnNewConnection
            End Enum

            Property Action As ActionEnum

            Property Context As Object

            Private ReadOnly _ex As Exception
            Public ReadOnly Property Exception() As Exception
                Get
                    Return _ex
                End Get
            End Property

            Private ReadOnly _cmd As Object
            Public ReadOnly Property Command() As Object
                Get
                    Return _cmd
                End Get
            End Property

            Private ReadOnly _mgr As OrmManager
            Public ReadOnly Property OrmManager() As OrmManager
                Get
                    Return _mgr
                End Get
            End Property

            Public Sub New(ex As Exception, cmd As Object, mgr As OrmManager)
                _ex = ex
                _cmd = cmd
                _mgr = mgr
            End Sub
        End Class
#End Region

        Public Delegate Function GetDictionaryDelegate(ByVal key As String) As IDictionary

        '<NonSerialized()>
        Public Event CacheDictionaryRequired(ByVal sender As QueryCmd, ByVal args As CacheDictionaryRequiredEventArgs)
        '<NonSerialized()>
        Public Event ExternalDictionary(ByVal sender As QueryCmd, ByVal args As ExternalDictionaryEventArgs)
        '<NonSerialized()>
        Public Event GetDynamicKey4Filter(ByVal sender As QueryCmd, ByVal args As GetDynamicKey4FilterEventArgs)
        '<NonSerialized()>
        Public Event QueryPrepared(ByVal sender As QueryCmd, ByVal args As QueryPreparedEventArgs)
        '<NonSerialized()>
        Public Event ModifyResult(ByVal sender As QueryCmd, ByVal args As ModifyResultArgs)

        Friend _sel As SelectClauseDef
        Protected _filter As IGetFilter
        Protected _group As GroupExpression
        Protected _order As OrderByClause
        'Protected _aggregates As ObjectModel.ReadOnlyCollection(Of AggregateBase)
        'Protected Friend _load As Boolean
        Protected _top As Top
        'Protected _page As Nullable(Of Integer)
        Protected _distinct As Boolean
        Protected _dontcache As Boolean
        Private _liveTime As TimeSpan
        Private _mgrMark As String
        Protected _clientPage As Paging
        Protected _pager As IPager
        Protected _joins() As QueryJoin
        Protected _autoJoins As Boolean = True
        Friend _from As FromClauseDef
        Protected _hint As String
        Protected _mark As Guid = Guid.NewGuid 'Environment.TickCount
        Protected _statementMark As Guid = Guid.NewGuid 'Environment.TickCount
        Protected _includeEntities As New List(Of EntityUnion)
        'Protected _realType As Type
        'Private _m2mObject As IKeyEntity
        'Protected _m2mKey As String
        Protected _rn As TableFilter
        Friend _outer As QueryCmd
        Private _er As OrmManager.ExecutionResult
        Private _includeFields As New Dictionary(Of String, Pair(Of Type, List(Of String)))
        Friend _resDic As Boolean
        Private _appendMain As Boolean?
        '<NonSerialized()> _
        Protected _getMgr As ICreateManager
        Private _name As String
        Private _execCnt As Integer
        '<NonSerialized()> _
        Private _schema As ObjectMappingEngine
        Private _context As IDictionary
        Friend _cacheSort As Boolean
        Private _autoFields As Boolean = True
        Private _timeout As Nullable(Of Integer)
        Private _poco As IDictionary
        Friend _pocoType As Type
        Friend _createTypes As New Dictionary(Of EntityUnion, EntityUnion)
        Private _unions As ReadOnlyCollection(Of Pair(Of Boolean, QueryCmd))
        Private _having As IGetFilter
        Friend _optimizeIn As IFilter
        Friend _optimizeOr As List(Of Criteria.PredicateLink)
        Private _newMaps As IDictionary
        Friend _notSimpleMode As Boolean
        Private _rsCnt As Integer = 0
        Private _exec As IExecutor

        '<NonSerialized>
        Friend _messages As EventArgs

        Private _oldStart As Integer
        Private _oldLength As Integer
        Private _oldRev As Boolean

        '<NonSerialized()>
        Public Event ConnectionException(sender As QueryCmd, args As ConnectionExceptionArgs)
        '<NonSerialized()>
        Public Event CommandException(sender As QueryCmd, args As CommandExceptionArgs)

        Private _fallBack As OrmManager.ApplyFilterFallBackDelegate
#Region " Cache "
        '<NonSerialized()> _
        Friend _types As Dictionary(Of EntityUnion, IEntitySchema)
        '<NonSerialized()> _
        Friend _cols As Dictionary(Of EntityUnion, List(Of SelectExpression))
        '<NonSerialized()> _
        Friend _sl As List(Of SelectExpression)
        '<NonSerialized()> _
        Friend _f As IFilter
        '<NonSerialized()> _
        Friend _js As List(Of QueryJoin)
        Friend _ftypes As Dictionary(Of EntityUnion, Object)
        Friend _stypes As Dictionary(Of EntityUnion, Object)
#End Region

        Public ReadOnly Property Messages As EventArgs
            Get
                Return _messages
            End Get
        End Property
        Public Function Include(ByVal propertyPath As String) As QueryCmd
            Dim mpe As ObjectMappingEngine = Nothing
            Dim t As Type = Nothing
            If CreateManager IsNot Nothing Then
                mpe = GetMappingEngine()
                t = GetSelectedType(mpe)
            End If
            Include(mpe, t, "^this", propertyPath)
            Return Me
        End Function

        Protected Sub Include(ByVal mpe As ObjectMappingEngine, ByVal t As Type, ByVal base As String, ByVal propertyPath As String)
            Dim ss() As String = propertyPath.Split("."c)
            Dim p As Pair(Of Type, List(Of String)) = Nothing
            If base <> "^this" Then
                Dim schema As IEntitySchema = GetEntitySchema(mpe, t)
                If _includeFields.TryGetValue(base, p) Then
                    t = p.First
                ElseIf t IsNot Nothing Then
                    Dim m As MapField2Column = Nothing
                    If Not schema.FieldColumnMap.TryGetValue(base, m) Then
                        Throw New QueryCmdException(String.Format("Cannot find property {0} in type {1}", ss(0), t), Me)
                    Else
                        t = m.PropertyInfo.PropertyType
                    End If
                Else
                    Throw New QueryCmdException(String.Format("You should specify selected type first"), Me)
                End If
            End If
            If t IsNot Nothing Then
                Dim schema As IEntitySchema = GetEntitySchema(mpe, t)
                Dim m As MapField2Column = Nothing
                If Not schema.FieldColumnMap.TryGetValue(ss(0), m) Then
                    Throw New QueryCmdException(String.Format("Cannot find property {0} in type {1}", ss(0), t), Me)
                End If
            End If
            If p Is Nothing AndAlso Not _includeFields.TryGetValue(base, p) Then
                p = New Pair(Of Type, List(Of String))(t, New List(Of String))
                _includeFields(base) = p
            End If
            If Not p.Second.Contains(ss(0)) Then
                p.Second.Add(ss(0))
            End If
            If ss.Length > 1 Then
                Include(mpe, t, ss(0), String.Join(".", ss, 1, ss.Length - 1))
            End If
        End Sub

        Public Sub OptimizeInFilter(ByVal inFilter As IFilter)
            _optimizeIn = inFilter
        End Sub

        Public Sub OptimizeInFilter(ByVal orFilters As List(Of Criteria.PredicateLink))
            _optimizeOr = orFilters
        End Sub

        Public Function LoadObjects(Of T As ICachedEntity)(ByVal entityList As Worm.ReadOnlyEntityList(Of T)) As ReadOnlyEntityList(Of T)
            Return LoadObjects(entityList, 0, entityList.Count)
        End Function

        Public Function LoadObjects(Of T As ICachedEntity)(ByVal entityList As Worm.ReadOnlyEntityList(Of T), ByVal start As Integer, ByVal length As Integer) As ReadOnlyEntityList(Of T)
            If length = 0 Then Return entityList
            If start >= entityList.Count Then
                Throw New ArgumentException(String.Format("Start value {0} greater than list length {1}", start, entityList.Count))
            End If

            Dim need2Load As Boolean = False
            Dim l As New List(Of EntityExpression)
            If SelectList IsNot Nothing Then
                For Each s As SelectExpression In SelectList
                    If TypeOf s.Expression Is EntityExpression Then
                        l.Add(CType(s.Expression, EntityExpression))
                    End If
                Next
            End If

            Using gm = GetManager(_getMgr)
                If gm Is Nothing Then
                    Throw New QueryCmdException("OrmManager required", Me)
                End If

                need2Load = PrepareLoad2Load(Of T)(entityList, start, length, l, gm.Manager.Cache, gm.Manager.MappingEngine)
            End Using

            If need2Load Then
                Return ToEntityList(Of T)()
            Else
                Return entityList
            End If
        End Function

        Public Shared Function LoadObjects(Of T As ICachedEntity)(ByVal entityList As Worm.ReadOnlyEntityList(Of T), ByVal withLoad As Boolean, ByVal mgr As OrmManager) As ReadOnlyEntityList(Of T)
            Return LoadObjects(entityList, 0, entityList.Count, withLoad, mgr)
        End Function

        Public Shared Function LoadObjects(Of T As ICachedEntity)(ByVal entityList As Worm.ReadOnlyEntityList(Of T), _
            ByVal start As Integer, ByVal length As Integer, ByVal withLoad As Boolean, ByVal mgr As OrmManager) As ReadOnlyEntityList(Of T)
            If length = 0 OrElse entityList.Count = 0 Then Return entityList

            If entityList Is Nothing Then
                Throw New ArgumentNullException("entityList")
            End If

            If start >= entityList.Count Then
                Throw New ArgumentException(String.Format("Start value {0} greater than list length {1}", start, entityList.Count))
            End If

            Dim q As New QueryCmd
            If q.SelectEntity(entityList.RealType, withLoad).From(entityList.RealType).PrepareLoad2Load(Of T)(entityList, start, length, mgr) Then
                Return q.ToEntityList(Of T)(mgr)
            Else
                Return entityList
            End If
        End Function

        Public Shared Function LoadObjects(Of T As ICachedEntity)(ByVal entityList As Worm.ReadOnlyEntityList(Of T), _
            ByVal start As Integer, ByVal length As Integer, ByVal properties2Load As ObjectModel.ReadOnlyCollection(Of SelectExpression), ByVal mgr As OrmManager) As ReadOnlyEntityList(Of T)
            If length = 0 Then Return entityList

            If entityList Is Nothing Then
                Throw New ArgumentNullException("entityList")
            End If

            If start >= entityList.Count Then
                Throw New ArgumentException(String.Format("Start value {0} greater than list length {1}", start, entityList.Count))
            End If

            Dim q As New QueryCmd
            If q.Select(properties2Load).From(entityList.RealType).PrepareLoad2Load(Of T)(entityList, start, length, mgr) Then
                Return q.ToEntityList(Of T)(mgr)
            Else
                Return entityList
            End If
        End Function

        Private Function PrepareLoad2Load(Of T As ICachedEntity)(ByVal entityList As Worm.ReadOnlyEntityList(Of T), ByVal start As Integer,
                                                                 ByVal length As Integer, ByVal properties As List(Of EntityExpression),
                                                                 ByVal cache As CacheBase, mpe As ObjectMappingEngine) As Boolean

            If properties Is Nothing OrElse properties.Count = 0 Then
                Return PrepareLoad2Load(OrmManager.FormPKValues(cache, entityList, start, length), entityList.RealType, mpe)
            Else
                Return PrepareLoad2Load(OrmManager.FormPKValues(cache, entityList, start, length, False, properties), entityList.RealType, mpe)
            End If
        End Function

        Private Function PrepareLoad2Load(Of T As ICachedEntity)(ByVal entityList As Worm.ReadOnlyEntityList(Of T), ByVal start As Integer,
                                                                 ByVal length As Integer, ByVal mgr As OrmManager) As Boolean

            Return PrepareLoad2Load(OrmManager.FormPKValues(mgr?.Cache, entityList, start, length), entityList.RealType, mgr?.MappingEngine)
        End Function

        Private Function PrepareLoad2Load(ByVal e2load As IEnumerable(Of ICachedEntity), ByVal realType As Type, mpe As ObjectMappingEngine) As Boolean

            If e2load.Count = 0 Then Return False

            Dim pa As String = Nothing
            Dim ids As New List(Of Object)
            Dim pks As New List(Of IPKDesc)
            Dim oschema = mpe.GetEntitySchema(realType)
            Dim tbl = oschema.GetPK.Table
            For Each o As ICachedEntity In e2load
                Dim pk = o.GetPKValues(Nothing)
                If pk.Count = 1 Then
                    pa = pk(0).Column
                    ids.Add(pk(0).Value)
                Else
                    pks.Add(pk)
                End If
            Next

            If ids.Count > 0 Then
                OptimizeInFilter(Ctor.column(tbl, pa).in(ids).Filter)
            ElseIf pks.Count > 0 Then
                Dim gpp As New List(Of Criteria.PredicateLink)
                For Each pk In pks
                    Dim pp As Criteria.PredicateLink = Nothing
                    For Each p In pk
                        If pp Is Nothing Then
                            pp = Ctor.column(tbl, p.Column).eq(p.Value)
                        Else
                            pp = pp.and(tbl, p.Column).eq(p.Value)
                        End If
                    Next
                    gpp.Add(pp)
                Next
                OptimizeInFilter(gpp)
            Else
                Throw New InvalidOperationException
            End If

            Return True
        End Function

        Friend ReadOnly Property GetBatchStruct() As Pair(Of List(Of Object), FieldReference)
            Get
                If _optimizeIn Is Nothing Then Return Nothing

                Dim tmf As TemplatedFilterBase = CType(_optimizeIn, TemplatedFilterBase)
                Dim ftemp As TableFilterTemplate = TryCast(tmf.Template, TableFilterTemplate)
                Dim fr As FieldReference = Nothing
                If ftemp IsNot Nothing Then
                    fr = New FieldReference(ftemp.Table, ftemp.Column)
                Else
                    Dim ef As OrmFilterTemplate = TryCast(tmf.Template, OrmFilterTemplate)
                    fr = New FieldReference(ef.ObjectSource, ef.PropertyAlias)
                End If
                Dim col As IEnumerable = CType(tmf.Value, InValue).Value
                If Not TypeOf col Is List(Of Object) Then
                    Dim l As New List(Of Object)
                    For Each oo As Object In col
                        If GetType(ISinglePKEntity).IsAssignableFrom(oo.GetType) Then
                            l.Add(CType(oo, ISinglePKEntity).Identifier)
                        Else
                            l.Add(oo)
                        End If
                    Next
                    col = l
                End If
                Return New Pair(Of List(Of Object), FieldReference)(CType(col, List(Of Object)), fr)
            End Get
        End Property

        Friend ReadOnly Property GetBatchOrStruct() As List(Of Criteria.PredicateLink)
            Get
                Return _optimizeOr
            End Get
        End Property

        Public ReadOnly Property CreateTypes() As Dictionary(Of EntityUnion, EntityUnion)
            Get
                Return _createTypes
            End Get
        End Property

        Public ReadOnly Property CreateType() As EntityUnion
            Get
                If _createTypes.Count = 1 Then
                    For Each ct As KeyValuePair(Of EntityUnion, EntityUnion) In _createTypes
                        Return ct.Value
                    Next
                End If
                Return Nothing
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

        Public Property CommandTimeout() As Nullable(Of Integer)
            Get
                Return _timeout
            End Get
            Set(ByVal value As Nullable(Of Integer))
                _timeout = value
            End Set
        End Property

        Public Property SpecificMappingEngine() As ObjectMappingEngine
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
        Public Property LoadResultsetThreadCount() As Integer
            Get
                Return _rsCnt
            End Get
            Set(ByVal value As Integer)
                _rsCnt = value
            End Set
        End Property
        Public Property LastExecutionResult() As OrmManager.ExecutionResult
            Get
                Return _er
            End Get
            Protected Friend Set(ByVal value As OrmManager.ExecutionResult)
                _er = value
            End Set
        End Property

        Public Function GetMappingEngine(Optional throwIfNotFound As Boolean = False) As ObjectMappingEngine
            If SpecificMappingEngine IsNot Nothing Then
                Return SpecificMappingEngine
            ElseIf _getMgr IsNot Nothing Then
                Using gm = GetManager(_getMgr)
                    If gm IsNot Nothing Then
                        Return gm.Manager.MappingEngine
                    ElseIf throwIfNotFound Then
                        Throw New QueryCmdException("OrmManager required", Me)
                    End If
                End Using
            ElseIf throwIfNotFound Then
                Throw New QueryCmdException("OrmManager required", Me)
            End If

            Return Nothing
        End Function

        Public Property CreateManager() As ICreateManager
            Get
                Return _getMgr
            End Get
            Set(value As ICreateManager)
                _getMgr = value
            End Set
        End Property

        Protected Friend Sub RaiseExternalDictionary(ByVal args As ExternalDictionaryEventArgs)
            RaiseEvent ExternalDictionary(Me, args)
        End Sub

        'Protected Friend Sub SetSelectList(ByVal l As ObjectModel.ReadOnlyCollection(Of OrmProperty))
        '    _fields = l
        'End Sub

        '<NonSerialized()> _

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

        Protected Friend Property AppendMain() As Boolean?
            Get
                Return _appendMain
            End Get
            Set(ByVal value As Boolean?)
                _appendMain = value
            End Set
        End Property

        Protected Sub OnSortChanged()
            RenewStatementMark()
        End Sub

#Region " Ctors "
        Public Sub New()

        End Sub

        Public Sub New(ByVal getMgr As CreateManagerDelegate)
            Me.New()
            _getMgr = New CreateManager(getMgr)
        End Sub

        Public Sub New(ByVal getMgr As ICreateManager)
            Me.New()
            _getMgr = getMgr
        End Sub
#End Region

        Protected Friend Sub RenewMark()
            _mark = Guid.NewGuid 'Environment.TickCount
            '_dic = Nothing
        End Sub

        Protected Sub RenewStatementMark()
            _statementMark = Guid.NewGuid 'Environment.TickCount
        End Sub

        Private Class cls2
            Inherits svct

            Public Sub New(ByVal cmd As QueryCmd)
                MyBase.New(cmd)

                If cmd.SelectedEntities IsNot Nothing Then
                    Dim t As New List(Of Pair(Of EntityUnion, Boolean?))
                    For Each tp As Pair(Of EntityUnion, Boolean?) In _types
                        t.Add(New Pair(Of EntityUnion, Boolean?)(tp.First, False))
                    Next
                    cmd._sel = New SelectClauseDef(t)
#If nlog Then
                    'NLog.LogManager.GetCurrentClassLogger?.Trace("_cmd._sel type {0}", Environment.StackTrace)
#End If
                End If
            End Sub

            Public Sub cl_paging(ByVal sender As IExecutor, ByVal args As IExecutor.GetCacheItemEventArgs)
                args.ForceLoad = True
                SetCT2Nothing()
                RemoveHandler sender.OnGetCacheItem, AddressOf Me.cl_paging
            End Sub
        End Class

        Private Function IsAnonymous(ByVal mpe As ObjectMappingEngine) As Boolean
            Dim isanonym As Boolean
            If CreateType IsNot Nothing Then
                Dim ct As Type = CreateType.GetRealType(mpe)
                isanonym = GetType(AnonymousEntity).IsAssignableFrom(ct) _
                    AndAlso Not GetType(AnonymousCachedEntity).IsAssignableFrom(ct)
            End If
            Return isanonym
        End Function

        Public Shared Sub Prepare(ByVal root As QueryCmd, ByVal executor As IExecutor, _
            dx As IDataContext)

            Prepare(root, executor, dx.MappingEngine, dx.Context, dx.StmtGenerator)
        End Sub

        Public Shared Sub Prepare(ByVal root As QueryCmd, ByVal executor As IExecutor, _
            ByVal mpe As ObjectMappingEngine, ByVal contextInfo As IDictionary, _
            ByVal stmt As StmtGenerator)

            Dim isanonym As Boolean = root.IsAnonymous(mpe)

            'Dim fs As New List(Of IFilter)
            For Each q As QueryCmd In New StmtQueryIterator(root)
                'Dim j As New List(Of Worm.Criteria.Joins.QueryJoin)
                'Dim c As List(Of SelectExpression) = Nothing
                q.Prepare(executor, mpe, contextInfo, stmt, isanonym)
                'If f IsNot Nothing Then
                '    fs.Add(f)
                'End If

                'js.Add(j)
                'cs.Add(c)
            Next

            'Return fs.ToArray
        End Sub

        Private Shared Sub Prepare(ByVal outer As QueryCmd, ByVal root As QueryCmd, ByVal executor As IExecutor, _
                    ByVal mpe As ObjectMappingEngine, ByVal contextInfo As IDictionary, _
                    ByVal stmt As StmtGenerator)

            Dim isanonym As Boolean = root.IsAnonymous(mpe)

            For Each q As QueryCmd In New StmtQueryIterator(root)
                Dim old As QueryCmd = q._outer
                Try
                    q._outer = outer
                    q.Prepare(executor, mpe, contextInfo, stmt, isanonym)
                Finally
                    q._outer = old
                End Try
            Next

        End Sub

        Private Sub AddExpression2SelectList(ByVal se As SelectExpression, ByVal mpe As ObjectMappingEngine, isAnonym As Boolean)
            _sl.Add(se)
            If (_outer IsNot Nothing OrElse _rn IsNot Nothing) AndAlso String.IsNullOrEmpty(se.ColumnAlias) Then
                Dim pa = se.GetIntoPropertyAlias
                If String.IsNullOrEmpty(pa) AndAlso Not isAnonym Then
                    'Dim t As Type = Nothing
                    'If se.ObjectProperty.Entity IsNot Nothing Then
                    '    t = se.ObjectProperty.Entity.GetRealType(mpe)
                    'End If
                    'If t Is Nothing Then
                    '    se.ColumnAlias = "[" & se.GetIntoPropertyAlias & "]"
                    'End If
                    se.ColumnAlias = "[cl" & _sl.Count & "]"
                Else
                    se.ColumnAlias = "[" & pa & "]"
                End If
            End If
            If TypeOf se.Operand Is PropertyAliasExpression AndAlso String.IsNullOrEmpty(se.GetIntoPropertyAlias) Then
                se.IntoPropertyAlias = CType(se.Operand, PropertyAliasExpression).PropertyAlias
            End If
        End Sub

        Protected Sub PrepareSelectList(ByVal executor As IExecutor, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean, ByVal mpe As ObjectMappingEngine, _
                                        ByRef f As IFilter, ByVal filterInfo As IDictionary)
            If isAnonym Then
                For Each se As SelectExpression In SelectList
                    se.Prepare(executor, mpe, filterInfo, stmt, isAnonym)
                    AddExpression2SelectList(se, mpe, isAnonym)
                    Dim t As Type = Nothing
                    Dim into As EntityUnion = se.GetIntoEntityUnion
                    If into IsNot Nothing Then
                        t = into.GetRealType(mpe)
                        If Not _types.ContainsKey(into) Then
                            _types.Add(into, mpe.GetEntitySchema(t))
                        End If
                    End If

                    If _poco IsNot Nothing Then
                        If t Is Nothing Then
                            For Each tp As Type In _poco.Keys
                                t = tp
                                Exit For
                            Next
                        End If
                        'If GetType(AnonymousCachedEntity).IsAssignableFrom(t) Then
                        'Else
                        se.IntoPropertyAlias = t.Name & "-" & se.GetIntoPropertyAlias
                        'End If
                    End If

                    If String.IsNullOrEmpty(se.GetIntoPropertyAlias) AndAlso TypeOf se.Operand Is TableExpression Then
                        se.IntoPropertyAlias = CType(se.Operand, TableExpression).SourceField
                    End If
                    CheckFrom(se)
                Next
            Else
                If IsFTS AndAlso GetSelectedTypesCount(mpe) > 1 Then
                    _appendMain = True
                End If

                For Each se As SelectExpression In SelectList
                    se.Prepare(executor, mpe, filterInfo, stmt, isAnonym)
                    Dim os As EntityUnion = se.GetIntoEntityUnion
                    If os IsNot Nothing Then
                        If Not _types.ContainsKey(os) Then
                            _types.Add(os, mpe.GetEntitySchema(os.GetRealType(mpe)))
                        End If
                    End If
                    CheckFrom(se)
                Next

                If _types.Count > 0 Then
                    For Each de As KeyValuePair(Of EntityUnion, IEntitySchema) In _types
                        Dim oschema As IEntitySchema = de.Value
                        If Not _cols.ContainsKey(de.Key) Then
                            Dim cols As New List(Of SelectExpression)
                            _cols.Add(de.Key, cols)

                            Dim col As List(Of SelectExpression) = GetSelectList(de.Key)
                            If col.Count > 0 Then
                                If AutoFields AndAlso _outer Is Nothing AndAlso Not _notSimpleMode AndAlso Not IsDistinct AndAlso _group Is Nothing Then
                                    Dim _createType As EntityUnion = Nothing
                                    If Not _createTypes.TryGetValue(de.Key, _createType) Then
l2:
                                        'For Each m As MapField2Column In oschema.GetPKs
                                        If True Then
                                            Dim m = oschema.GetPK
                                            Dim pa As String = m.PropertyAlias
                                            If Not col.Exists(Function(c) c.GetIntoPropertyAlias = pa) Then
                                                Dim se As New SelectExpression(de.Key, pa)
                                                If Not _sl.Contains(se) Then
                                                    se.Attributes = m.Attributes
                                                    cols.Add(se)
                                                    AddExpression2SelectList(se, mpe, isAnonym)
                                                End If
                                            End If
                                            'Next
                                        End If
                                    Else
                                        Dim ct As Type = _createType.GetRealType(mpe)
                                        Dim t As Type = de.Key.GetRealType(mpe)
                                        If GetType(AnonymousEntity) IsNot ct AndAlso (t Is ct OrElse EntityUnion.EntityNameEquals(mpe, _createType, de.Key)) Then
                                            GoTo l2
                                        End If
                                    End If
                                End If
                                Dim df As IDefferedLoading = TryCast(oschema, IDefferedLoading)
                                If df IsNot Nothing Then
                                    Dim loadGroups()() As String = df.GetDefferedLoadPropertiesGroups
                                    If loadGroups IsNot Nothing Then
                                        For Each se As SelectExpression In col
                                            Dim pa As String = se.GetIntoPropertyAlias
                                            If Not Array.Exists(loadGroups, Function(loadProperties) _
                                                Array.Exists(loadProperties, Function(pr) pr = pa)) Then

                                                cols.Add(se)
                                                AddExpression2SelectList(se, mpe, isAnonym)
                                            End If
                                        Next
                                        GoTo l1
                                    End If
                                End If

                                For Each se As SelectExpression In col
                                    AddExpression2SelectList(se, mpe, isAnonym)
                                    cols.Add(se)
                                Next
l1:
                            End If
                        End If
                    Next

                    If _sl.Count = 0 Then
                        For Each se As SelectExpression In SelectList
                            AddExpression2SelectList(se, mpe, isAnonym)
                        Next
                    Else
                        For Each se As SelectExpression In SelectList
                            If Not _sl.Contains(se) Then AddExpression2SelectList(se, mpe, isAnonym)
                        Next
                    End If

                    If AutoJoins Then
                        Dim selOS As EntityUnion = GetSelectedOS()
                        If selOS IsNot Nothing Then
                            Dim t As Type = selOS.GetRealType(mpe)
                            Dim selSchema As IEntitySchema = Nothing
                            If Not _types.TryGetValue(selOS, selSchema) Then
                                Dim fromEU As EntityUnion = _from.GetFromEntity
                                If fromEU IsNot Nothing Then
                                    selSchema = mpe.GetEntitySchema(fromEU.GetRealType(mpe))
                                    'If Not _types.ContainsKey(fromEU) Then
                                    _types.Add(fromEU, selSchema)
                                    'End If
                                End If
                            End If

                            For Each se As SelectExpression In SelectList
                                Dim en As IEnumerable(Of SelectUnion) = GetSelectedEntities(se)
                                For Each su As SelectUnion In en
                                    Dim os As EntityUnion = su.EntityUnion
                                    If os IsNot Nothing Then
                                        If Not HasInQuery(os) Then
                                            Dim jt As Type = os.GetRealType(mpe)
                                            mpe.AppendJoin(selOS, t, selSchema, _
                                                os, jt, _types(se.GetIntoEntityUnion), _
                                                f, _js, filterInfo, JoinType.Join)
                                        End If
                                    Else
                                        If Not HasInQuery(su.SourceFragment) Then
                                            Throw New NotSupportedException("Cannot auto join " & se.GetDynamicString)
                                        End If
                                    End If
                                Next
                            Next
                        End If
                    End If
                Else
                    For Each se As SelectExpression In SelectList
                        AddExpression2SelectList(se, mpe, isAnonym)
                    Next
                End If
            End If
        End Sub

        Protected Overridable Sub _Prepare(ByVal executor As IExecutor,
                                           ByVal mpe As ObjectMappingEngine, ByVal contextInfo As IDictionary,
                                           ByVal stmt As StmtGenerator, ByRef filter As IFilter, ByVal selectOS As EntityUnion,
                                           ByVal isAnonym As Boolean)

            If _from IsNot Nothing Then
                Dim anq As QueryCmd = _from.AnyQuery
                If anq IsNot Nothing Then
                    Try
                        'anq._outer = Me
                        Prepare(Me, anq, executor, mpe, contextInfo, stmt)
                    Finally
                        'anq._outer = Nothing
                    End Try
                End If
            End If

            'словарь дочерне-родительских связей
            'в качестве ключа используется юнион дочернего тип + дочерняя пропертя + тип парента
            'в качестве значения - пара: дочерняя пропертя + юнион парента
            Dim child2parentRelation As New Dictionary(Of String, Pair(Of String, EntityUnion))

            If SelectList IsNot Nothing Then
                PrepareSelectList(executor, stmt, isAnonym, mpe, filter, contextInfo)

                For Each eu As EntityUnion In _includeEntities
                    AddTypeFields(mpe, _sl, New Pair(Of EntityUnion, Boolean?)(eu, True), Nothing, isAnonym, stmt)

                    If AutoJoins Then
                        Dim selOS As EntityUnion = GetSelectedOS()
                        If selOS IsNot Nothing Then
                            Dim t As Type = selOS.GetRealType(mpe)
                            Dim selSchema As IEntitySchema = _types(selOS) 'mpe.GetEntitySchema(t)
                            If Not HasInQuery(eu) Then
                                Dim jt As Type = eu.GetRealType(mpe)
                                mpe.AppendJoin(selOS, t, selSchema,
                                    eu, jt, _types(eu),
                                    filter, _js, contextInfo, JoinType.Join)
                            End If
                        End If
                    End If
                Next
            Else
                'If IsFTS Then
                '    For Each tp As Pair(Of EntityUnion, Boolean?) In SelectedEntities
                '        Dim os As EntityUnion = tp.First
                '        Dim rt As Type = tp.First.GetRealType(mpe)
                '        Dim oschema As IEntitySchema = mpe.GetEntitySchema(rt)
                '        If _WithLoad(tp, mpe) Then
                '            _appendMain = True
                '            _sl.AddRange(mpe.GetSortedFieldList(rt, oschema).ConvertAll(Function(c As EntityPropertyAttribute) ObjectMappingEngine.ConvertColumn2SelExp(c, os)))
                '        Else
                '            Dim ctx As IContextObjectSchema = TryCast(oschema, IContextObjectSchema)
                '            If ctx IsNot Nothing Then
                '                Dim cf As IFilter = ctx.GetContextFilter(filterInfo)
                '                If cf IsNot Nothing Then
                '                    _appendMain = True
                '                    _sl.AddRange(mpe.GetPrimaryKeys(rt, oschema).ConvertAll(Function(c As EntityPropertyAttribute) ObjectMappingEngine.ConvertColumn2SelExp(c, os)))
                '                    Continue For
                '                End If
                '            End If

                '            Dim jb As IJoinBehavior = TryCast(oschema, IJoinBehavior)
                '            If jb IsNot Nothing AndAlso jb.AlwaysJoinMainTable Then
                '                _appendMain = True
                '                _sl.AddRange(mpe.GetPrimaryKeys(rt, oschema).ConvertAll(Function(c As EntityPropertyAttribute) ObjectMappingEngine.ConvertColumn2SelExp(c, os)))
                '                Continue For
                '            End If

                '            Dim pk As String = mpe.GetPrimaryKeys(rt, oschema)(0).PropertyAlias
                '            Dim se As New SelectExpression(_from.Table, stmt.FTSKey, pk)
                '            se.Into = os
                '            se.Attributes = Field2DbRelations.PK
                '            _sl.Add(se)
                '        End If
                '        If Not _types.ContainsKey(tp.First) Then
                '            _types.Add(tp.First, oschema)
                '        End If
                '    Next
                'Else
                For Each eu As EntityUnion In _includeEntities
                    AddEntityToSelectList(eu, True)
                Next

                If SelectedEntities IsNot Nothing Then
l1:
#If DEBUG Then
                    If _sl.Count > 0 Then
                        Throw New InvalidOperationException
                    End If

                    If _types.Count > 0 Then
                        Throw New InvalidOperationException
                    End If
#End If
                    If _from IsNot Nothing AndAlso _from.Query IsNot Nothing Then
                        If SelectedEntities.Count > 1 Then
                            Throw New NotSupportedException
                        Else
                            AddTypeFields(mpe, _sl, SelectedEntities(0), Nothing, isAnonym, stmt)
                        End If
                    Else
                        Dim selTypes As ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?)) = SelectedEntities
                        If selTypes Is Nothing Then
                            If _from IsNot Nothing Then
                                selTypes = New ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))(New Pair(Of EntityUnion, Boolean?)() {New Pair(Of EntityUnion, Boolean?)(_from.ObjectSource, Nothing)})
#If nlog Then
                                'NLog.LogManager.GetCurrentClassLogger?.Trace("From dump: {0}", _from.ObjectSource.Dump(mpe))
#End If
                            Else
                                Throw New QueryCmdException("Neither SelectTypes nor FromClause not set", Me)
                            End If
                        End If
                        For Each tp As Pair(Of EntityUnion, Boolean?) In selTypes
                            Dim p As Pair(Of String, EntityUnion) = Nothing
                            For Each d As KeyValuePair(Of String, Pair(Of String, EntityUnion)) In child2parentRelation
                                If d.Value.Second Is tp.First Then
                                    p = d.Value
                                End If
                            Next
                            AddTypeFields(mpe, _sl, tp, p?.First, isAnonym, stmt)
                            'If tp.Second Then
                            '    Throw New NotImplementedException
                            'Else
                            '    Dim pk As String = schema.GetPrimaryKeys(tp.First.GetRealType(schema))(0).PropertyAlias
                            '    Dim se As New SelectExpression(tp.First, pk)
                            '    se.Attributes = Field2DbRelations.PK
                            '    cl.Add(se)
                            'End If
                            'If Not _types.ContainsKey(tp.First) Then
                            '    Dim t As Type = tp.First.GetRealType(schema)
                            '    _types.Add(tp.First, schema.GetEntitySchema(t))
                            'End If
                        Next

                        If _from Is Nothing Then
                            _from = New FromClauseDef(selTypes(0).First)
                        End If

                        For Each de As KeyValuePair(Of EntityUnion, IEntitySchema) In _types
                            Dim deEU As EntityUnion = de.Key
                            Dim t As Type = deEU.GetRealType(mpe)
                            Dim oschema As IEntitySchema = de.Value
                            Dim parentTypeAdded As Boolean = False
                            For Each m As MapField2Column In oschema.FieldColumnMap
                                Dim pi As Reflection.PropertyInfo = m.PropertyInfo
                                Dim parentType As Type = pi?.PropertyType

                                'если тип парента не сущность, продолжаем цикл
                                If Not ObjectMappingEngine.IsEntityType(parentType) Then Continue For

                                Dim pa As String = m.PropertyAlias

                                'если парент не поддерживает lazy load, его надо загрузить вместе с дочерним объектом
                                If Not GetType(IPropertyLazyLoad).IsAssignableFrom(parentType) Then
                                    'загрузить только если это свойство (парент) реально выбирается
                                    If _sl.Exists(Function(se) se.GetIntoPropertyAlias = pa AndAlso se.GetIntoEntityUnion = deEU) Then
                                        parentTypeAdded = AddParentTypeToQuery(mpe, contextInfo, filter, child2parentRelation, de, parentType, t, pa)
                                    End If
                                Else
                                    If deEU = selTypes(0).First Then
                                        Dim p As Pair(Of Type, List(Of String)) = Nothing
                                        If _includeFields.TryGetValue("^this", p) AndAlso p IsNot Nothing Then
                                            'Dim ep As EntityPropertyAttribute = CType(tde.Key, EntityPropertyAttribute)
                                            If p.Second.Contains(pa) Then
                                                parentTypeAdded = AddParentTypeToQuery(mpe, contextInfo, filter, child2parentRelation, de, parentType, t, pa)
                                                If Not parentTypeAdded AndAlso Not _sl.Exists(Function(se) se.GetIntoPropertyAlias = pa AndAlso se.GetIntoEntityUnion = deEU) Then
                                                    _sl.Add(New SelectExpression(deEU, pa) With {
                                                        .Attributes = m.Attributes
                                                    })
                                                End If
                                            End If
                                        End If
                                    Else
                                        For Each p As Pair(Of String, EntityUnion) In child2parentRelation.Values
                                            If p.Second Is deEU Then
                                                Dim prop As String = p.First
                                                Dim ip As Pair(Of Type, List(Of String)) = Nothing
                                                If _includeFields.TryGetValue(prop, ip) AndAlso ip IsNot Nothing Then
                                                    If ip.Second.Contains(pa) Then
                                                        parentTypeAdded = AddParentTypeToQuery(mpe, contextInfo, filter, child2parentRelation, de, parentType, t, pa)
                                                    End If
                                                End If

                                                Exit For
                                            End If
                                        Next
                                    End If
                                End If
                            Next

                            If parentTypeAdded Then
                                _sl = New List(Of SelectExpression)
                                _types = New Dictionary(Of EntityUnion, IEntitySchema)
                                GoTo l1
                            End If
                        Next
                    End If
                Else
                    'Dim s As IEntitySchema = GetSchemaForSelectType(schema)
                    'If _from IsNot Nothing AndAlso _from.Table IsNot Nothing AndAlso s IsNot Nothing Then
                    '    For Each m As MapField2Column In s.GetFieldColumnMap
                    '        Dim se As New SelectExpression(m.Table, m.Column)
                    '        se.Attributes = m.Attributes
                    '        se.PropertyAlias = m._propertyAlias
                    '        _sl.Add(se)
                    '    Next
                    '    'Else
                    '    '    Throw New NotSupportedException
                    'Else
                    If _from Is Nothing Then
                        Dim s As IEntitySchema = GetSchemaForSelectType(mpe)
                        If s IsNot Nothing Then
                            _from = New FromClauseDef(s.Table)
                        End If
                    End If
                    If _poco IsNot Nothing Then
                        For Each de As DictionaryEntry In _poco
                            AddEntityToSelectList(New EntityUnion(CType(de.Key, Type)), Nothing)
                            GoTo l1
                        Next
                    End If
                    'If GetType(AnonymousEntity).IsAssignableFrom(_createType) Then
                    '    Throw New QueryCmdException("Neither SelectTypes nor SelectList specified", Me)
                    'End If
                End If
                'End If

                Dim fromEU As EntityUnion = _from?.GetFromEntity
                If fromEU IsNot Nothing Then
                    If Not _types.ContainsKey(fromEU) Then
                        _types.Add(fromEU, mpe.GetEntitySchema(fromEU.GetRealType(mpe)))
                    End If
                End If

                If AutoJoins OrElse IsFTS Then
                    Dim t As Type = Nothing
                    Dim selSchema As IEntitySchema = Nothing
                    Dim tos As EntityUnion = fromEU
                    If tos Is Nothing Then
                        tos = selectOS
                    End If
                    If tos IsNot Nothing AndAlso SelectedEntities IsNot Nothing Then
                        t = tos.GetRealType(mpe)
                        selSchema = _types(tos)

                        For Each tp As Pair(Of EntityUnion, Boolean?) In SelectedEntities
                            If Not EntityUnion.TypeEquals(mpe, tp.First, tos) AndAlso Not HasInQuery(tp.First) Then
                                If (_appendMain Is Nothing OrElse Not _appendMain) AndAlso IsFTS Then
                                    _appendMain = True
                                End If

                                If _js.Find(Function(join) join.ObjectSource = tp.First) Is Nothing Then
                                    mpe.AppendJoin(tos, t, selSchema,
                                                   tp.First, tp.First.GetRealType(mpe), mpe.GetEntitySchema(tp.First.GetRealType(mpe)),
                                                   filter, _js, contextInfo, JoinType.Join)
                                End If
                            End If
                        Next
                    End If
                End If
            End If

            _f = filter
        End Sub

        Private Function AddParentTypeToQuery(ByVal mpe As ObjectMappingEngine, ByVal contextInfo As IDictionary,
                                              ByRef f As IFilter, ByVal child2parentRelation As Dictionary(Of String, Pair(Of String, EntityUnion)),
                                              ByVal de As KeyValuePair(Of EntityUnion, IEntitySchema), ByVal parentType As Type,
                                              ByVal childType As Type, ByVal childPropertyAlias As String) As Boolean
            Dim parentEU As EntityUnion = Nothing
            Dim p As Pair(Of String, EntityUnion) = Nothing
            Dim inQuery As Boolean = False
            Dim key As String = de.Key._ToString & "$" & childPropertyAlias & "$" & parentType.ToString
            If Not child2parentRelation.TryGetValue(key, p) Then
                parentEU = New EntityUnion(New QueryAlias(parentType))
                child2parentRelation(key) = New Pair(Of String, EntityUnion)(childPropertyAlias, parentEU)
            Else
                parentEU = p.Second
            End If
            If Not HasInQuery(parentEU) Then
                Dim s As IEntitySchema = Nothing
                If Not GetType(IEntity).IsAssignableFrom(parentType) Then
                    Dim hasPK As Boolean
                    s = GetPOCOSchema(mpe, parentType, hasPK)
                    AddPOCO(parentType, s)
                Else
                    s = mpe.GetEntitySchema(parentType, False)
                End If
                mpe.AppendJoin(de.Key, childType, de.Value, parentEU, parentType, s, f, _js, JoinType.LeftOuterJoin, contextInfo, ObjectMappingEngine.JoinFieldType.Direct, childPropertyAlias)
                inQuery = True
                AddEntityToSelectList(parentEU, True)
            End If
            Return inQuery
        End Function

        Friend _prepared As Boolean
        Friend _cancel As Boolean

        Public Sub Prepare(ByVal executor As IExecutor,
                           ByVal schema As ObjectMappingEngine, ByVal filterInfo As IDictionary,
                           ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements Worm.Criteria.Values.IQueryElement.Prepare

            _sl = New List(Of SelectExpression)
            _types = New Dictionary(Of EntityUnion, IEntitySchema)
            _cols = New Dictionary(Of EntityUnion, List(Of SelectExpression))
            _js = New List(Of QueryJoin)
            _ftypes = New Dictionary(Of EntityUnion, Object)
            _stypes = New Dictionary(Of EntityUnion, Object)
            _newMaps = Nothing

            If Joins IsNot Nothing Then
                '_js.AddRange(Joins)
                For Each j As QueryJoin In Joins
                    _js.Add(j)
                    j.Prepare(executor, schema, filterInfo, stmt, isAnonym)
                Next
            End If

            Dim f As IFilter = Nothing
            If Filter IsNot Nothing Then
                'f = Filter.Filter(selectType)
                f = Filter.Filter()
            End If

            If _order IsNot Nothing Then
                For Each s As SortExpression In _order
                    s.Prepare(executor, schema, filterInfo, stmt, isAnonym)
                    For Each su As SelectUnion In GetSelectedEntities(s)
                        If su.EntityUnion IsNot Nothing Then
                            If Not _stypes.ContainsKey(su.EntityUnion) Then
                                _stypes.Add(su.EntityUnion, Nothing)
                            End If
                        End If
                    Next
                Next
            End If

            If f IsNot Nothing Then
                For Each fl As IFilter In f.GetAllFilters
                    fl.Prepare(executor, schema, filterInfo, stmt, isAnonym)
                    Dim ef As EntityFilter = TryCast(fl, EntityFilter)
                    If ef IsNot Nothing Then
                        'Dim tp As Type = ef.Template.ObjectSource.GetRealType(schema)
                        If Not _ftypes.ContainsKey(ef.Template.ObjectSource) Then
                            _ftypes.Add(ef.Template.ObjectSource, Nothing)
                        End If
                    End If
                Next
            End If

            If Not ClientPaging.IsEmpty OrElse _pager IsNot Nothing Then
                If executor Is Nothing Then
                    Throw New QueryCmdException("Client paging is not supported in this mode", Me)
                End If
                AddHandler executor.OnGetCacheItem, AddressOf New cls2(Me).cl_paging
            End If

            Dim selectOS As EntityUnion = GetSelectedOS()

            If selectOS IsNot Nothing Then
                Dim selectType As Type = selectOS.GetRealType(schema)

                If selectType Is Nothing Then
                    Throw New QueryCmdException(String.Format("selectOS {0} has null type", selectOS.Dump(schema)), Me)
                End If

                If AutoJoins Then
                    Dim joins() As Worm.Criteria.Joins.QueryJoin = Nothing
                    Dim appendMain As Boolean
                    If HasJoins(schema, selectType, f, Sort, filterInfo, joins, appendMain, selectOS) Then
                        _js.AddRange(joins)
                    End If
                    _appendMain = _appendMain OrElse appendMain
                End If

            End If

            _Prepare(executor, schema, filterInfo, stmt, f, selectOS, isAnonym)

            _prepared = True

            Dim args As New QueryPreparedEventArgs
            RaiseEvent QueryPrepared(Me, args)
            _cancel = args.Cancel
        End Sub

        Protected Friend Function HasJoins(ByVal schema As ObjectMappingEngine, ByVal selectType As Type,
                                           ByRef filter As IFilter, ByVal s As OrderByClause, ByVal contextInfo As IDictionary, ByRef joins() As QueryJoin,
                                           ByRef appendMain As Boolean, ByVal selectOS As EntityUnion) As Boolean
            Dim l As New List(Of QueryJoin)
            Dim oschema As IEntitySchema = schema.GetEntitySchema(selectType)
            Dim ictx As IContextObjectSchema = TryCast(oschema, IContextObjectSchema)
            If ictx IsNot Nothing AndAlso ictx.GetContextFilter(contextInfo) IsNot Nothing Then
                appendMain = True
            End If
            Dim types As New List(Of Type)
            If filter IsNot Nothing Then
                For Each fl As IFilter In filter.Filter.GetAllFilters
                    Dim f As IEntityFilter = TryCast(fl, IEntityFilter)
                    If f IsNot Nothing Then
                        Dim ot As OrmFilterTemplate = CType(f.Template, OrmFilterTemplate)
                        If FromClause IsNot Nothing AndAlso ot.ObjectSource = FromClause.ObjectSource Then
                            Continue For
                        End If

                        If _js.Find(Function(join) join.ObjectSource = ot.ObjectSource) Is Nothing Then
                            Dim type2join As System.Type = ot.ObjectSource.GetRealType(schema)

                            If type2join Is Nothing Then
                                Throw New QueryCmdException(String.Format("ot.ObjectSource {0} has null type", ot.ObjectSource.Dump(schema)), Me)
                            End If

                            If (selectType.IsAssignableFrom(type2join) OrElse type2join.IsAssignableFrom(selectType)) AndAlso
                                oschema.FieldColumnMap.TryGetValue(ot.PropertyAlias, Nothing) Then
                                Continue For
                            End If

                            'Try
                            AppendJoin(schema, selectType, filter, contextInfo, appendMain, l, oschema, types, ot.ObjectSource, type2join, selectOS)
                            'Catch ex As OrmManagerException When selectType.IsAssignableFrom(type2join) OrElse type2join.IsAssignableFrom(selectType)
                            'do nothing
                            'End Try
                        End If
                    Else
                        Dim cf As CustomFilter = TryCast(fl, CustomFilter)
                        If cf IsNot Nothing Then
                            Dim cfv As CustomValue = CType(cf.Template, CustomValue)
                            For Each se As SelectUnion In GetSelectedEntities(cfv.Values)
                                If se.EntityUnion IsNot Nothing Then
                                    Dim seeu As EntityUnion = se.EntityUnion
                                    If _js.Find(Function(join) join.ObjectSource = seeu) Is Nothing Then
                                        AppendJoin(schema, selectType, filter, contextInfo, appendMain, l, oschema, types,
                                            se.EntityUnion, se.EntityUnion.GetRealType(schema), selectOS)
                                    End If
                                End If
                            Next
                        End If
                    End If
                Next
            End If

            If s IsNot Nothing Then
                For Each ns As SortExpression In s
                    For Each se As SelectUnion In GetSelectedEntities(ns)
                        If se.EntityUnion IsNot Nothing Then
                            If _js.Find(Function(join) join.ObjectSource = se.EntityUnion) IsNot Nothing Then
                                Continue For
                            End If
                            Dim sortType As System.Type = se.EntityUnion.GetRealType(schema)
                            If sortType IsNot selectType AndAlso sortType IsNot Nothing AndAlso Not types.Contains(sortType) Then
                                Dim field As String = oschema.GetJoinFieldNameByType(sortType)

                                If Not String.IsNullOrEmpty(field) Then
                                    types.Add(sortType)
                                    l.Add(OrmManager.MakeJoin(schema, se.EntityUnion, sortType, selectOS, field, Criteria.FilterOperation.Equal, JoinType.Join))
                                    Continue For
                                End If

                                If String.IsNullOrEmpty(field) Then
                                    Dim sortSchema As IEntitySchema = schema.GetEntitySchema(sortType)
                                    field = sortSchema.GetJoinFieldNameByType(selectType)

                                    If Not String.IsNullOrEmpty(field) Then
                                        types.Add(sortType)
                                        l.Add(OrmManager.MakeJoin(schema, selectOS, selectType, se.EntityUnion, field, Criteria.FilterOperation.Equal, JoinType.Join, True))
                                        Continue For
                                    End If
                                End If

                                If String.IsNullOrEmpty(field) Then
                                    Dim m2m As M2MRelationDesc = schema.GetM2MRelation(sortType, selectType, True)
                                    If m2m IsNot Nothing Then
                                        l.AddRange(OrmManager.MakeM2MJoin(schema, m2m, sortType))
                                    ElseIf Not (sortType.IsAssignableFrom(selectType) OrElse selectType.IsAssignableFrom(sortType)) Then
                                        Throw New OrmManagerException(String.Format("Relation {0} to {1} is ambiguous or not exist. Specify joins explicity", selectType, sortType))
                                    End If
                                End If

                            ElseIf sortType Is selectType OrElse sortType Is Nothing Then
                                appendMain = True
                            End If
                        End If
                    Next
                Next
            End If
            joins = l.ToArray
            Return joins.Length > 0
        End Function

        Private Shared Sub AppendJoin(ByVal schema As ObjectMappingEngine, ByVal selectType As Type, _
            ByRef filter As IFilter, ByVal contextInfo As IDictionary, ByRef appendMain As Boolean, _
            ByVal l As List(Of QueryJoin), ByVal oschema As IEntitySchema, ByVal types As List(Of Type), _
            ByVal joinOS As EntityUnion, ByVal type2join As System.Type, ByVal selectOS As EntityUnion)

            'Dim type2join As System.Type = joinOS.GetRealType(schema)

            If type2join Is Nothing Then
                Throw New NullReferenceException("Type for OrmFilterTemplate must be specified")
            End If

            If selectType Is type2join Then
                appendMain = True
            Else
                Dim s2 As IEntitySchema = schema.GetEntitySchema(type2join)
                If oschema.Equals(s2) OrElse oschema.GetType.FullName = s2.GetType.FullName Then
                    appendMain = True
                Else
                    If Not types.Contains(type2join) Then
                        OrmManager.AppendJoin(schema, selectType, filter, contextInfo, l, oschema, types, type2join, s2, joinOS, selectOS)
                    End If
                End If
            End If
        End Sub

        Protected Friend Function Need2Join(ByVal eu As EntityUnion) As Boolean
            Return True '_ftypes.ContainsKey(eu) OrElse _stypes.ContainsKey(eu) OrElse _types.ContainsKey(eu)
        End Function

        Protected Friend Function Need2MainType(ByVal eu As EntityUnion) As Boolean
            Dim r As Boolean = _ftypes.ContainsKey(eu) OrElse _stypes.ContainsKey(eu) OrElse _from Is Nothing OrElse _from.ObjectSource Is Nothing OrElse _from.ObjectSource.Equals(eu)
            If Not r Then
                For Each j As QueryJoin In Joins
                    If j.M2MObjectSource IsNot Nothing AndAlso j.ObjectSource IsNot Nothing AndAlso j.Condition Is Nothing AndAlso eu.Equals(j.ObjectSource) Then
                        Return False
                    End If
                Next
            End If
            Return r
        End Function

        Private Sub CheckFrom(ByVal se As SelectExpression)
            If _from Is Nothing Then
                For Each su As SelectUnion In GetSelectedEntities(se)
                    If su.EntityUnion IsNot Nothing Then
                        _from = New FromClauseDef(su.EntityUnion)
                    Else
                        _from = New FromClauseDef(su.SourceFragment)
                    End If
                    Exit For
                Next
            End If
        End Sub

        Protected Function HasInQuery(ByVal os As EntityUnion) As Boolean
            If FromClause IsNot Nothing AndAlso FromClause.ObjectSource IsNot Nothing AndAlso FromClause.ObjectSource.Equals(os) Then
                Return True
            End If
            Return HasInQueryJS(os)
        End Function

        Protected Function HasInQueryJS(ByVal os As EntityUnion) As Boolean
            For Each j As QueryJoin In _js
                If os.Equals(j.ObjectSource) OrElse os.Equals(j.M2MObjectSource) Then
                    Return True
                End If
            Next
            'If SelectedEntities IsNot Nothing Then
            '    For Each tp As Pair(Of EntityUnion, Boolean?) In SelectedEntities
            '        If os.Equals(tp.First) AndAlso (FromClause Is Nothing OrElse FromClause.ObjectSource Is Nothing OrElse Not FromClause.ObjectSource.Equals(os)) Then
            '            Return True
            '        End If
            '    Next
            'End If
            Return False
        End Function

        Protected Function HasInQuery(ByVal tbl As SourceFragment) As Boolean
            If FromClause IsNot Nothing AndAlso FromClause.Table Is tbl Then
                Return True
            End If
            For Each j As QueryJoin In _js
                If tbl Is j.Table Then
                    Return True
                End If
            Next
            Return False
        End Function

        Protected Sub AddTypeFields(ByVal mpe As ObjectMappingEngine, ByVal cl As List(Of SelectExpression),
                                    ByVal tp As Pair(Of EntityUnion, Boolean?), ByVal pref As String, ByVal isAnonym As Boolean, ByVal stmt As StmtGenerator)
            Dim t As Type = tp.First.GetRealType(mpe)

#If nlog Then
            'NLog.LogManager.GetCurrentClassLogger?.Trace("Select type {1} hash code: {0}. Dump: {2}", t.GetHashCode, t, tp.First.Dump(mpe))
#End If

            Dim oschema As IEntitySchema = GetEntitySchema(mpe, t)

            If oschema Is Nothing Then
                Throw New QueryCmdException(String.Format("Cannot find schema for type {0}", t), Me)
            End If

            Dim withLoad As Boolean = _WithLoad(tp, mpe)
            If Not GetType(IPropertyLazyLoad).IsAssignableFrom(t) OrElse withLoad Then
                _appendMain = withLoad AndAlso IsFTS

                Dim l As New List(Of SelectExpression)
                If FromClause IsNot Nothing AndAlso FromClause.QueryEU IsNot Nothing AndAlso FromClause.QueryEU.IsQuery Then
                    'l.AddRange(mpe.GetSortedFieldList(t, oschema).ConvertAll(Function(c As EntityPropertyAttribute) _
                    '    New SelectExpression(New PropertyAliasExpression(c.PropertyAlias)) With { _
                    '        .Into = tp.First, _
                    '        .Attributes = c.Behavior, _
                    '        .IntoPropertyAlias = c.PropertyAlias _
                    '    } _
                    '))
                    For Each mp As MapField2Column In oschema.GetAutoLoadFields
                        l.Add(New SelectExpression(New PropertyAliasExpression(mp.PropertyAlias)) With {
                            .Into = tp.First,
                            .Attributes = mp.Attributes,
                            .IntoPropertyAlias = mp.PropertyAlias
                        })
                    Next
                Else
                    For Each mp As MapField2Column In oschema.GetAutoLoadFields
                        l.Add(New SelectExpression(New ObjectProperty(tp.First, mp.PropertyAlias)))
                    Next
                    'l.AddRange(mpe.GetSortedFieldList(t, oschema).ConvertAll(Function(c As EntityPropertyAttribute) ObjectMappingEngine.ConvertColumn2SelExp(c, tp.First)))
                End If

                Dim df As IDefferedLoading = TryCast(oschema, IDefferedLoading)
                If df IsNot Nothing Then
                    Dim sss()() As String = df.GetDefferedLoadPropertiesGroups
                    If sss IsNot Nothing Then
                        For Each ss() As String In sss
                            For Each pr As String In ss
                                Dim pr2 As String = pr
                                Dim idx As Integer = l.FindIndex(Function(pa As SelectExpression) pa.GetIntoPropertyAlias = pr2)
                                If idx >= 0 Then
                                    l.RemoveAt(idx)
                                End If
                            Next
                        Next
                    End If
                End If
                Dim pod As Boolean = _poco IsNot Nothing AndAlso _poco.Contains(t)
                Dim hasPK As Boolean = cl.Find(Function(se) (se.Attributes And Field2DbRelations.PK) = Field2DbRelations.PK) IsNot Nothing
                For Each se As SelectExpression In l
                    Dim prop As String = se.GetIntoPropertyAlias
                    If pod Then
                        Dim rp As String = prop
                        Dim ee As EntityExpression = TryCast(se.Operand, EntityExpression)
                        If ee IsNot Nothing Then
                            rp = ee.ObjectProperty.PropertyAlias
                        End If
                        se.IntoPropertyAlias = t.Name & "-" & rp
                        If Not String.IsNullOrEmpty(pref) Then
                            se.IntoPropertyAlias = "%" & pref & "-" & se.IntoPropertyAlias
                        End If
                    End If
                    cl.Add(se)
                    Dim m As MapField2Column = oschema.FieldColumnMap(prop)
                    se.Attributes = se.Attributes Or m.Attributes
                    'If hasPK AndAlso (isAnonym OrElse CreateType Is Nothing OrElse CreateType.GetRealType(mpe) Is GetType(AnonymousCachedEntity)) Then
                    '    se.Attributes = se.Attributes And Not Field2DbRelations.PK
                    'End If
                Next
            Else
                If FromClause IsNot Nothing AndAlso FromClause.QueryEU IsNot Nothing AndAlso FromClause.QueryEU.IsQuery Then
                    'For Each c As MapField2Column In oschema.GetPKs
                    Dim c = oschema.GetPK
                    Dim se As New SelectExpression(New PropertyAliasExpression(c.PropertyAlias)) With {
                                .Into = tp.First, .IntoPropertyAlias = c.PropertyAlias
                            }
                    se.Attributes = c.Attributes
                    cl.Add(se)
                    'Next
                Else
                    If IsFTS Then
                        Dim pk As String = mpe.GetPrimaryKey(t, oschema)
                        Dim se As New SelectExpression(_from.Table, stmt.FTSKey, pk) With {
                            .Into = tp.First,
                            .Attributes = Field2DbRelations.PK
                        }
                        _sl.Add(se)
                    Else
                        'For Each c As MapField2Column In oschema.GetPKs
                        Dim c = oschema.GetPK
                        Dim se As New SelectExpression(tp.First, c.PropertyAlias) With {
                            .Attributes = c.Attributes
                        }
                        cl.Add(se)
                        'Next
                    End If
                End If
            End If

            If Not _types.ContainsKey(tp.First) Then
                _types.Add(tp.First, oschema)
            End If
        End Sub

        Public Shared Function GetStaticKey(ByVal root As QueryCmd, ByVal mgrKey As String, ByVal cb As Cache.CacheListBehavior, _
            ByVal fromKey As String, ByVal mpe As ObjectMappingEngine, _
            ByRef dic As IDictionary) As String
            Dim key As New StringBuilder

            Dim ca As CacheDictionaryRequiredEventArgs = Nothing
            Dim cb_ As Cache.CacheListBehavior = cb

            Dim i As Integer = 0
            For Each q As QueryCmd In New MetaDataQueryIterator(root)
                If i > 0 Then
                    key.Append("$nextq:")
                End If

                'Dim f As IFilter = _f
                'If fs.Length > i Then
                '    f = fs(i)
                'End If

                'Dim rt As Type = q.SelectedType
                'If rt Is Nothing AndAlso Not String.IsNullOrEmpty(q.SelectedEntityName) Then
                '    rt = mpe.GetTypeByEntityName(q.SelectedEntityName)
                'End If

                If Not q.GetStaticKey(key, cb_, mpe) Then
                    If ca Is Nothing Then
                        ca = New CacheDictionaryRequiredEventArgs
                        q.RaiseCacheDictionaryRequired(ca)
                        If ca.GetDictionary Is Nothing Then
                            If cb = Cache.CacheListBehavior.CacheOrThrowException Then
                                Throw New QueryCmdException("Cannot cache query", root)
                            Else
                                Return Nothing
                            End If
                        End If
                    End If
                    q.GetStaticKey(key, Cache.CacheListBehavior.CacheAll, mpe)
                    cb_ = Cache.CacheListBehavior.CacheAll
                End If
                i += 1
            Next

            If Not String.IsNullOrEmpty(fromKey) Then
                key.Append(fromKey).Append("$")
            End If

            key.Append("$").Append(mgrKey)

            If ca IsNot Nothing Then
                dic = ca.GetDictionary(key.ToString)
                Return Nothing
            Else
                Return key.ToString
            End If
        End Function

        Protected Sub RaiseCacheDictionaryRequired(ByVal ca As CacheDictionaryRequiredEventArgs)
            RaiseEvent CacheDictionaryRequired(Me, ca)
        End Sub

        ''' <summary>
        ''' Статические строки всех запчастей QueryCmd (фильтры, джоины и проч.)
        ''' </summary>
        ''' <param name="sb"></param>
        ''' <param name="cb"></param>
        ''' <param name="mpe"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Friend Function GetStaticKey(ByVal sb As StringBuilder,
                                               ByVal cb As Cache.CacheListBehavior,
                                               ByVal mpe As ObjectMappingEngine) As Boolean

            If Not _prepared Then
                Throw New QueryCmdException("Command not prepared", Me)
            End If

            Dim sb2 As New StringBuilder

            Dim f As IFilter = _f
            Dim j As List(Of QueryJoin) = _js

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

            'If _pod IsNot Nothing Then
            '    sb2.Append(_pod.First.ToString).Append("$")
            'End If

            If _from IsNot Nothing Then
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
                ElseIf _from.ObjectSource IsNot Nothing Then
                    'Dim t As Type = _from.ObjectSource.GetRealType(mpe)
                    'If t Is Nothing Then
                    '    _from.AnyQuery.GetStaticKey(sb, cb, mpe, fi)
                    'Else
                    '    sb.Append(mpe.GetEntityKey(fi, t))
                    'End If
                    sb.Append(_from.ObjectSource.ToStaticString(mpe))
                Else
                    sb.Append(_from.Query.ToStaticString(mpe))
                End If
            Else
                Throw New NotSupportedException
            End If

            sb2.Append("$")

            If cb <> Cache.CacheListBehavior.CacheAll Then
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

            If SelectList IsNot Nothing Then
                Dim it As IList = SelectList
                If GetSelectedTypesCount(mpe) < 2 OrElse IsAnonymous(mpe) Then
                    Dim l As New List(Of SelectExpression)(SelectList)
                    l.Sort(Function(s1 As SelectExpression, s2 As SelectExpression) s1.GetDynamicString.CompareTo(s2.GetDynamicString))
                    it = l
                End If
                For Each c As SelectExpression In it
                    If Not GetStaticKeyFromProp(sb, cb, c, mpe) Then
                        Return False
                    End If
                Next
                sb.Append("$")
            ElseIf SelectedEntities IsNot Nothing Then
                For Each t As Pair(Of EntityUnion, Boolean?) In SelectedEntities
                    sb.Append(t.First.ToStaticString(mpe))
                Next
                sb.Append("$")
            End If

            If _group IsNot Nothing Then
                sb.Append(_group.GetStaticString(mpe)).Append("$")
            End If

            If _having IsNot Nothing Then
                sb.Append(_having.Filter.GetStaticString(mpe)).Append("$")
            End If

            If _order IsNot Nothing Then
                If CacheSort OrElse _top IsNot Nothing OrElse cb <> Cache.CacheListBehavior.CacheAll Then
                    For Each n As SortExpression In Sort
                        sb.Append(n.GetStaticString(mpe))
                    Next
                    sb.Append("$")
                End If
            End If

            Return True
        End Function

        Private Shared Function GetStaticKeyFromProp(ByVal sb As StringBuilder, ByVal cb As Cache.CacheListBehavior, _
            ByVal c As SelectExpression, ByVal mpe As ObjectMappingEngine) As Boolean
            'If c.IsCustom OrElse c.Query IsNot Nothing Then
            '    Dim dp As Cache.IDependentTypes = Cache.QueryDependentTypes(mpe, c)
            '    If Not Cache.IsCalculated(dp) Then
            '        Select Case cb
            '            Case Cache.CacheListBehavior.CacheAll
            '                'do nothing
            '                'Case Cache.CacheListBehavior.CacheOrThrowException
            '            Case Cache.CacheListBehavior.CacheWhatCan, Cache.CacheListBehavior.CacheOrThrowException
            '                Return False
            '            Case Else
            '                Throw New NotSupportedException(String.Format("Cache behavior {0} is not supported", cb.ToString))
            '        End Select
            '    End If
            'End If
            sb.Append(c.GetStaticString(mpe))
            Return True
        End Function

        Public Shared Function GetDynamicKey(ByVal root As QueryCmd, mpe As ObjectMappingEngine) As String
            Dim id As New StringBuilder

            Dim i As Integer = 0
            For Each q As QueryCmd In New MetaDataQueryIterator(root)
                If i > 0 Then
                    id.Append("$nextq:")
                End If
                'Dim f As IFilter = Nothing
                'If fs.Length > i Then
                '    f = fs(i)
                'End If
                q.GetDynamicKey(id, mpe)
                i += 1
            Next

            Return id.ToString '& GetType(T).ToString
        End Function

        ''' <summary>
        ''' Динамические строк (включающие значения) всех запчастей QueryCmd
        ''' </summary>
        ''' <param name="sb"></param>
        ''' <remarks></remarks>
        Protected Friend Sub GetDynamicKey(ByVal sb As StringBuilder, mpe As ObjectMappingEngine)
            If Not _prepared Then
                Throw New QueryCmdException("Command not prepared", Me)
            End If

            Dim f As IFilter = _f
            Dim j As List(Of QueryJoin) = _js

            If f IsNot Nothing Then
                Dim args As New GetDynamicKey4FilterEventArgs(f)
                RaiseEvent GetDynamicKey4Filter(Me, args)
                If Not String.IsNullOrEmpty(args.CustomKey) Then
                    sb.Append(args.CustomKey).Append("f$")
                Else
                    sb.Append(f._ToString).Append("f$")
                End If
            End If

            If _optimizeIn IsNot Nothing Then
                sb.Append(_optimizeIn._ToString).Append("i$")
            End If

            If _optimizeOr IsNot Nothing Then
                For Each f In _optimizeOr
                    sb.Append(f._ToString)
                Next
                sb.Append("o$")
            End If

            If _having IsNot Nothing Then
                sb.Append(_having.Filter._ToString).Append("h$")
            End If

            If j IsNot Nothing Then
                For Each join As QueryJoin In j
                    If Not QueryJoin.IsEmpty(join) Then
                        sb.Append(join._ToString)
                    End If
                Next
                sb.Append("j$")
            End If

            If _top IsNot Nothing Then
                sb.Append(_top.GetDynamicKey).Append("t$")
            End If

            If _from IsNot Nothing Then
                If _from.ObjectSource IsNot Nothing Then
                    sb.Append(_from.ObjectSource._ToString())
                ElseIf _from.Query IsNot Nothing Then
                    sb.Append(_from.Query._ToString())
                End If
            Else
                Throw New NotSupportedException
            End If

            sb.Append("fr$")

            If _rn IsNot Nothing Then
                sb.Append(_rn.ToString).Append("rn$")
            End If

            If _order IsNot Nothing Then
                If mpe Is Nothing Then
                    mpe = GetMappingEngine()
                End If

                If mpe Is Nothing OrElse Not _order.CanEvaluate(mpe) Then
                    sb.Append(_order.ToString).Append("s$")
                End If
            End If
        End Sub

        Public Overrides Function ToString() As String
            Throw New NotSupportedException
        End Function

        Public Function ToStaticString(ByVal mpe As ObjectMappingEngine) As String
            Dim sb As New StringBuilder
            Dim l As List(Of SelectExpression) = Nothing
            GetStaticKey(sb, Cache.CacheListBehavior.CacheAll, mpe)
            'If SelectTypes IsNot Nothing Then
            '    For Each tp As Pair(Of ObjectSource, Boolean?) In SelectTypes
            '        sb.Append(tp.First.ToStaticString)
            '    Next
            '    sb.Append("$")

            'End If
            Return sb.ToString
        End Function

        Public Function GetSelectedType(ByVal mpe As ObjectMappingEngine) As Type
            Dim os As EntityUnion = GetSelectedOS()
            If os IsNot Nothing Then
                Return os.GetRealType(mpe)
            Else
                Return Nothing
            End If
        End Function

        Protected Function GetRealSelectedOS() As EntityUnion
            If SelectedEntities IsNot Nothing AndAlso SelectedEntities.Count > 0 Then
                Return SelectedEntities(0).First
            ElseIf _from IsNot Nothing AndAlso _from.ObjectSource IsNot Nothing Then
                Return _from.ObjectSource
            ElseIf _from IsNot Nothing AndAlso _from.Table IsNot Nothing AndAlso GetType(SearchFragment).IsAssignableFrom(_from.Table.GetType) Then
                Dim sf As SearchFragment = CType(_from.Table, SearchFragment)
                If sf.Entity IsNot Nothing Then Return sf.Entity
            End If
            Return Nothing
        End Function

        Public Function GetSelectedOS() As EntityUnion
            Dim os As EntityUnion = GetRealSelectedOS()
            If os Is Nothing AndAlso SelectList IsNot Nothing Then
                For Each s As SelectExpression In SelectList
                    For Each su As SelectUnion In GetSelectedEntities(s)
                        If su.EntityUnion IsNot Nothing Then
                            Return su.EntityUnion
                        End If
                    Next
                Next
            End If
            Return os
        End Function

        Friend Function NeedSelectType(ByVal mpe As ObjectMappingEngine) As Boolean
            If SelectedEntities IsNot Nothing Then
                Return False
            End If

            'If SelectList IsNot Nothing Then
            '    For Each s As SelectExpression In SelectList
            '        If s.ObjectProperty.ObjectSource Is Nothing Then
            '            Return False
            '        Else
            '            Dim t As Type = s.ObjectSource.GetRealType(mpe)
            '            If t Is Nothing OrElse GetType(AnonymousEntity).IsAssignableFrom(t) Then
            '                Return False
            '            End If
            '        End If
            '    Next
            'End If

            Return SelectList Is Nothing
        End Function

        Public Function GetSelectedTypes(ByVal mpe As ObjectMappingEngine, ByRef ts As ICollection(Of Type)) As Boolean
            'If _dic Is Nothing Then
            '    _dic = New Dictionary(Of ObjectSource, IEntitySchema)
            ts = New List(Of Type)

            If SelectedEntities IsNot Nothing Then
                For Each p As Pair(Of EntityUnion, Boolean?) In SelectedEntities
                    Dim os As EntityUnion = p.First
                    'If Not _dic.ContainsKey(os) Then
                    '    _dic.Add(os, Nothing)
                    'End If
                    ts.Add(os.GetRealType(mpe))
                Next
            ElseIf SelectList IsNot Nothing Then
                For Each s As SelectExpression In SelectList
                    For Each su As SelectUnion In GetSelectedEntities(s)
                        Dim os As EntityUnion = su.EntityUnion
                        If os IsNot Nothing Then
                            Dim t As Type = os.GetRealType(mpe)
                            If t Is Nothing OrElse GetType(AnonymousEntity).IsAssignableFrom(t) Then
                                '_dic.Clear()
                                ts.Clear()
                                Exit For
                                'ElseIf Not _dic.ContainsKey(s.ObjectProperty.ObjectSource) Then
                                '    _dic.Add(s.ObjectProperty.ObjectSource, Nothing)
                            ElseIf Not ts.Contains(t) Then
                                ts.Add(t)
                            End If
                        End If
                    Next
                Next
            End If

            Return ts.Count > 0
            'End If
            'Dim l As New List(Of Type)
            'For Each os As ObjectSource In _dic.Keys
            '    Dim t As Type = os.GetRealType(mpe)
            '    If Not l.Contains(t) Then
            '        l.Add(t)
            '    End If
            'Next
            'ts = l
            ''ts = Array.ConvertAll(Array.a _dic
            'Return l.Count > 0

        End Function

        Protected Function _WithLoad(ByVal os As EntityUnion, ByVal mpe As ObjectMappingEngine) As Boolean
            If SelectedEntities IsNot Nothing Then
                For Each tp As Pair(Of EntityUnion, Boolean?) In SelectedEntities
                    If tp.First.Equals(os) Then
                        Return _WithLoad(tp, mpe)
                    End If
                Next
            End If

            Return False
        End Function

        Protected Function _WithLoad(ByVal tp As Pair(Of EntityUnion, Boolean?), ByVal mpe As ObjectMappingEngine) As Boolean
            Return (tp.Second.HasValue AndAlso tp.Second.Value) OrElse Not GetType(IPropertyLazyLoad).IsAssignableFrom(tp.First.GetRealType(mpe))
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

        Public ReadOnly Property IsRealFTS() As Boolean
            Get
                If _from IsNot Nothing AndAlso _from.Table IsNot Nothing AndAlso GetType(SearchFragment).IsAssignableFrom(_from.Table.GetType) Then
                    Return True
                End If

                If _from IsNot Nothing AndAlso _from.AnyQuery IsNot Nothing AndAlso _from.AnyQuery.IsFTS Then
                    Return True
                End If

                If Joins IsNot Nothing Then
                    For Each j As QueryJoin In Joins
                        If j.ObjectSource IsNot Nothing AndAlso j.ObjectSource.IsQuery AndAlso j.ObjectSource.ObjectAlias.Query.IsFTS Then
                            Return True
                        End If
                    Next
                End If

                Return False
            End Get
        End Property

        Public Property FromClause() As FromClauseDef
            Get
                Return _from
            End Get
            Set(ByVal value As FromClauseDef)
                _from = value
                RenewMark()
            End Set
        End Property

        Public Property SelectClause() As SelectClauseDef
            Get
                Return _sel
            End Get
            Set(ByVal value As SelectClauseDef)
                _sel = value
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

        Friend Sub OnDataAvailable(ByVal mgr As OrmManager, ByVal er As OrmManager.ExecutionResult)
            _pager.SetTotalCount(er.RowCount)
            _oldStart = mgr._start
            mgr._start = _pager.GetCurrentPageOffset
            _oldLength = mgr._length
            mgr._length = _pager.GetPageSize
            _oldRev = mgr._rev
            mgr._rev = _pager.GetReverse
            RemoveHandler mgr.DataAvailable, AddressOf OnDataAvailable
        End Sub

        Friend Sub OnRestoreDefaults(ByVal e As IExecutor, ByVal mgr As OrmManager, ByVal args As EventArgs)
            mgr._start = _oldStart
            mgr._length = _oldLength
            mgr._rev = _oldRev
            RemoveHandler e.OnRestoreDefaults, AddressOf OnRestoreDefaults
        End Sub

        Public Property ContextInfo As IDictionary
            Get
                Return _context
            End Get
            Set(value As IDictionary)
                _context = value
            End Set
        End Property
        Public Property Pager() As IPager
            Get
                Return _pager
            End Get
            Set(ByVal value As IPager)
                _pager = value
            End Set
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

        Public Property Group() As GroupExpression
            Get
                Return _group
            End Get
            Set(ByVal value As GroupExpression)
                _group = value
                RenewMark()
            End Set
        End Property

        Public Property Joins() As QueryJoin()
            Get
                Return _joins
            End Get
            Set(ByVal value As QueryJoin())
                _joins = value
                RenewMark()
            End Set
        End Property

        Public Property Unions() As ReadOnlyCollection(Of Pair(Of Boolean, QueryCmd))
            Get
                Return _unions
            End Get
            Set(ByVal value As ReadOnlyCollection(Of Pair(Of Boolean, QueryCmd)))
                _unions = value
                RenewMark()
            End Set
        End Property

        Public Overridable Property SelectedEntities() As ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))
            Get
                If _sel IsNot Nothing Then
                    Return _sel.SelectTypes
                End If
                Return Nothing
            End Get
            Set(ByVal value As ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?)))
                SelectClause = New SelectClauseDef(value)
            End Set
        End Property

        Protected Function GetSelectList(ByVal os As EntityUnion) As List(Of SelectExpression)
            Dim l As New List(Of SelectExpression)
            For Each se As SelectExpression In SelectList
                If os.Equals(se.GetIntoEntityUnion) Then
                    l.Add(se)
                End If
            Next
            Return l
        End Function

        Protected Function GetSelectedTypesCount(ByVal mpe As ObjectMappingEngine) As Integer
            Dim l As New List(Of EntityUnion)
            If SelectList IsNot Nothing Then
                For Each se As SelectExpression In SelectList
                    Dim eu As EntityUnion = se.GetIntoEntityUnion
                    If eu IsNot Nothing AndAlso Not l.Contains(eu) Then
                        l.Add(eu)
                    End If
                Next
            End If
            Return l.Count
        End Function

        Public Property SelectList() As ObjectModel.ReadOnlyCollection(Of SelectExpression)
            Get
                If _sel IsNot Nothing Then
                    Return _sel.SelectList
                End If
                Return Nothing
            End Get
            Set(ByVal value As ObjectModel.ReadOnlyCollection(Of SelectExpression))
                SelectClause = New SelectClauseDef(value)
            End Set
        End Property

        Public Property Sort() As OrderByClause
            Get
                Return _order
            End Get
            Set(ByVal value As OrderByClause)
                _order = value
                RenewMark()
                'AddHandler value.OnChange, AddressOf OnSortChanged
            End Set
        End Property

        Public Property TopParam() As Top
            Get
                Return _top
            End Get
            Set(ByVal value As Top)
                _top = value
                RenewMark()
            End Set
        End Property

        Public Property IsDistinct() As Boolean
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

        Public Property HavingFilter() As IGetFilter
            Get
                Return _having
            End Get
            Set(ByVal value As IGetFilter)
                _having = value
                RenewMark()
            End Set
        End Property

        Protected Friend ReadOnly Property propWithLoad() As Boolean
            Get
                Return SelectedEntities IsNot Nothing AndAlso SelectedEntities(0).Second IsNot Nothing AndAlso SelectedEntities(0).Second.Value
            End Get
        End Property

        Protected Friend ReadOnly Property propWithLoads() As Boolean()
            Get
                If SelectedEntities Is Nothing Then
                    Dim r(_types.Count - 1) As Boolean
                    Return r
                Else
                    Dim r(SelectedEntities.Count - 1) As Boolean
                    For i As Integer = 0 To SelectedEntities.Count - 1
                        Dim t As Pair(Of EntityUnion, Boolean?) = SelectedEntities(i)
                        r(i) = t.Second.HasValue AndAlso t.Second.Value
                    Next
                    Return r
                End If
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

        Public Sub ClearJoins()
            Joins = New QueryJoin() {}
        End Sub

        '<Obsolete("User Join method")> _
        'Public Function JoinAdd(ByVal joins() As QueryJoin) As QueryCmd
        '    Dim l As New List(Of QueryJoin)
        '    If Me.Joins IsNot Nothing Then
        '        l.AddRange(Me.Joins)
        '    End If
        '    If joins IsNot Nothing Then
        '        l.AddRange(joins)
        '    End If
        '    Me.Joins = l.ToArray
        '    Return Me
        'End Function

        Public Function Join(ByVal rel As RelationDescEx) As QueryCmd
            Return Join(JCtor.join_relation(rel))
        End Function

        Public Function Join(ByVal joins() As QueryJoin) As QueryCmd
            'Me.Joins = joins
            Dim l As New List(Of QueryJoin)
            If Me.Joins IsNot Nothing Then
                l.AddRange(Me.Joins)
            End If
            If joins IsNot Nothing Then
                l.AddRange(joins)
            End If
            Me.Joins = l.ToArray
            Return Me
        End Function

        Public Function Distinct(ByVal value As Boolean) As QueryCmd
            IsDistinct = value
            Return Me
        End Function

        Public Function Top(ByVal value As Integer) As QueryCmd
            TopParam = New Query.Top(value)
            Return Me
        End Function

        Public Function OrderByNone() As QueryCmd
            Sort = Nothing
            Return Me
        End Function

        Public Function OrderBy(ByVal value As OrderByClause) As QueryCmd
            Sort = value
            Return Me
        End Function

        Public Function OrderBy(ByVal ParamArray op() As ObjectProperty) As QueryCmd
            Dim s As New SCtor.Int
            For Each o As ObjectProperty In op
                s = s.prop(o)
            Next
            Return OrderBy(s)
        End Function

        Public Function OrderBy(ByVal ParamArray exp() As ECtor.Int) As QueryCmd
            Dim f As New List(Of SortExpression)
            For Each ei As ECtor.Int In exp
                For Each e As IGetExpression In ei.AsEnumerable
                    If TypeOf e Is SortExpression Then
                        f.Add(CType(e, SortExpression))
                    Else
                        f.Add(New SortExpression(e.Expression))
                    End If
                Next
            Next
            Return OrderBy(New OrderByClause(f))
        End Function

        Public Function WhereOr(ByVal filter As IGetFilter) As QueryCmd
            If Me.Filter Is Nothing Then
                Me.Filter = filter
            ElseIf filter IsNot Nothing Then
                Me.Filter = Ctor.Filter(filter).or(Me.Filter)
            End If
            Return Me
        End Function

        Public Function WhereAnd(ByVal filter As IGetFilter) As QueryCmd
            If Me.Filter Is Nothing Then
                Me.Filter = filter
            ElseIf filter IsNot Nothing Then
                Me.Filter = Ctor.Filter(filter).and(Me.Filter)
            End If
            Return Me
        End Function

        Public Function Where(ByVal filter As IGetFilter) As QueryCmd
            Me.Filter = filter
            Return Me
        End Function

        Public Function HavingAdd(ByVal filter As IGetFilter) As QueryCmd
            If Me.HavingFilter Is Nothing Then
                Me.HavingFilter = filter
            Else
                Me.HavingFilter = Ctor.Filter(filter).and(Me.HavingFilter)
            End If
            Return Me
        End Function

        Public Function Having(ByVal filter As IGetFilter) As QueryCmd
            Me.HavingFilter = filter
            Return Me
        End Function

        Public Function Into(ByVal t As Type) As QueryCmd
            Dim seleu As EntityUnion = GetSelectedOS()
            If seleu Is Nothing Then
                'If GetType(AnonymousEntity).IsAssignableFrom(t) Then
                seleu = New EntityUnion(t)
                _createTypes(seleu) = seleu
                Return Me
                'Else
                '    Throw New QueryCmdException("Single selected entity required", Me)
                'End If
            End If
            _createTypes(seleu) = New EntityUnion(t)
            Return Me
        End Function

        Public Function Into(ByVal entityName As String) As QueryCmd
            Dim seleu As EntityUnion = GetSelectedOS()
            If seleu Is Nothing Then
                seleu = New EntityUnion(entityName)
                _createTypes(seleu) = seleu
                Return Me
                'Throw New QueryCmdException("Single selected entity required", Me)
            End If
            _createTypes(seleu) = New EntityUnion(entityName)
            Return Me
        End Function

        Public Function Into(ByVal eu As EntityUnion) As QueryCmd
            Dim seleu As EntityUnion = GetSelectedOS()
            If seleu Is Nothing Then
                _createTypes(eu) = eu
                Return Me
                'Throw New QueryCmdException("Single selected entity required", Me)
            End If
            _createTypes(seleu) = eu
            Return Me
        End Function

#Region " From "

        Public Function From(ByVal f As FromClauseDef) As QueryCmd
            _from = f
            RenewMark()
            Return Me
        End Function

        Public Function From(ByVal t As Type) As QueryCmd
            _from = New FromClauseDef(t)
            RenewMark()
            Return Me
        End Function
        Public Function From(ByVal t As Type, hint As String) As QueryCmd
            _from = New FromClauseDef(t, hint)
            RenewMark()
            Return Me
        End Function

        Public Function From(ByVal [alias] As QueryAlias) As QueryCmd
            _from = New FromClauseDef([alias])
            RenewMark()
            Return Me
        End Function

        Public Function From(ByVal entityName As String) As QueryCmd
            _from = New FromClauseDef(entityName)
            RenewMark()
            Return Me
        End Function
        Public Function From(ByVal entityName As String, hint As String) As QueryCmd
            _from = New FromClauseDef(entityName, hint)
            RenewMark()
            Return Me
        End Function

        Public Function From(ByVal os As EntityUnion) As QueryCmd
            _from = New FromClauseDef(os)
            RenewMark()
            Return Me
        End Function

        Public Function From(ByVal t As SourceFragment) As QueryCmd
            _from = New FromClauseDef(t)
            RenewMark()
            Return Me
        End Function

        Public Function From(ByVal q As QueryCmd) As QueryCmd
            _from = New FromClauseDef(q)
            RenewMark()
            Return Me
        End Function

#End Region

#Region " From search "

        Public Function FromSearch(ByVal searchType As Type, ByVal searchString As String) As QueryCmd
            Return From(New SearchFragment(searchType, searchString))
        End Function

        Public Function FromSearch(ByVal searchType As Type, ByVal searchString As String, ByVal top As Integer) As QueryCmd
            Return From(New SearchFragment(searchType, searchString, top))
        End Function

        Public Function FromSearch(ByVal searchType As Type, ByVal searchString As String, _
               ByVal search As SearchType) As QueryCmd
            Return From(New SearchFragment(searchType, searchString, search))
        End Function

        Public Function FromSearch(ByVal searchType As Type, ByVal searchString As String, _
               ByVal search As SearchType, ByVal top As Integer) As QueryCmd
            Return From(New SearchFragment(searchType, searchString, search, top))
        End Function

        Public Function FromSearch(ByVal searchType As Type, ByVal searchString As String, _
                         ByVal ParamArray queryFields() As String) As QueryCmd
            Return From(New SearchFragment(searchType, searchString, queryFields))
        End Function

        Public Function FromSearch(ByVal searchType As Type, ByVal searchString As String, _
                       ByVal search As SearchType, ByVal ParamArray queryFields() As String) As QueryCmd
            Return From(New SearchFragment(searchType, searchString, search, queryFields))
        End Function

        Public Function FromSearch(ByVal searchType As Type, ByVal searchString As String, _
            ByVal search As SearchType, ByVal top As Integer, _
            ByVal ParamArray queryFields() As String) As QueryCmd

            Return From(New SearchFragment(searchType, searchString, search, queryFields, top))
        End Function

        Public Function FromSearch(ByVal searcheu As String, ByVal searchString As String) As QueryCmd
            Return From(New SearchFragment(New EntityUnion(searcheu), searchString))
        End Function

        Public Function FromSearch(ByVal searcheu As String, ByVal searchString As String, ByVal top As Integer) As QueryCmd
            Return From(New SearchFragment(New EntityUnion(searcheu), searchString, top))
        End Function

        Public Function FromSearch(ByVal searcheu As String, ByVal searchString As String, _
               ByVal search As SearchType) As QueryCmd
            Return From(New SearchFragment(New EntityUnion(searcheu), searchString, search))
        End Function

        Public Function FromSearch(ByVal searcheu As String, ByVal searchString As String, _
               ByVal search As SearchType, ByVal top As Integer) As QueryCmd
            Return From(New SearchFragment(New EntityUnion(searcheu), searchString, search, top))
        End Function

        Public Function FromSearch(ByVal searcheu As String, ByVal searchString As String, _
                         ByVal ParamArray queryFields() As String) As QueryCmd
            Return From(New SearchFragment(New EntityUnion(searcheu), searchString, queryFields))
        End Function

        Public Function FromSearch(ByVal searcheu As String, ByVal searchString As String, _
                       ByVal search As SearchType, ByVal ParamArray queryFields() As String) As QueryCmd
            Return From(New SearchFragment(New EntityUnion(searcheu), searchString, search, queryFields))
        End Function

        Public Function FromSearch(ByVal searcheu As String, ByVal searchString As String, _
            ByVal search As SearchType, ByVal top As Integer, _
            ByVal ParamArray queryFields() As String) As QueryCmd

            Return From(New SearchFragment(New EntityUnion(searcheu), searchString, search, queryFields, top))
        End Function

        Public Function FromSearch(ByVal eu As EntityUnion, ByVal searchString As String) As QueryCmd
            Return From(New SearchFragment(eu, searchString))
        End Function

        Public Function FromSearch(ByVal eu As EntityUnion, ByVal searchString As String, ByVal top As Integer) As QueryCmd
            Return From(New SearchFragment(eu, searchString, top))
        End Function

        Public Function FromSearch(ByVal eu As EntityUnion, ByVal searchString As String, _
               ByVal search As SearchType) As QueryCmd
            Return From(New SearchFragment(eu, searchString, search))
        End Function

        Public Function FromSearch(ByVal eu As EntityUnion, ByVal searchString As String, _
               ByVal search As SearchType, ByVal top As Integer) As QueryCmd
            Return From(New SearchFragment(eu, searchString, search, top))
        End Function

        Public Function FromSearch(ByVal eu As EntityUnion, ByVal searchString As String, _
               ByVal search As SearchType, ByVal top As Integer, _
               ByVal ParamArray queryFields() As String) As QueryCmd
            Return From(New SearchFragment(eu, searchString, search, queryFields, top))
        End Function

        Public Function FromSearch(ByVal eu As EntityUnion, ByVal searchString As String, _
               ByVal search As SearchType, ByVal ParamArray queryFields() As String) As QueryCmd
            Return From(New SearchFragment(eu, searchString, search, queryFields))
        End Function

        Public Function FromSearch(ByVal eu As EntityUnion, ByVal searchString As String, _
            ByVal ParamArray queryFields() As String) As QueryCmd

            Return From(New SearchFragment(eu, searchString, queryFields))
        End Function

        Public Function FromSearch(ByVal searchString As String, ByVal top As Integer) As QueryCmd
            Return From(New SearchFragment(searchString, top))
        End Function

        Public Function FromSearch(ByVal searchString As String, ByVal searchType As SearchType, ByVal top As Integer) As QueryCmd
            Return From(New SearchFragment(searchString, searchType, top))
        End Function

        Public Function FromSearch(ByVal searchString As String, ByVal searchType As SearchType, _
                       ByVal top As Integer, ByVal ParamArray queryFields() As String) As QueryCmd
            Return From(New SearchFragment(searchString, searchType, top, queryFields))
        End Function

        Public Function FromSearch(ByVal searchString As String, ByVal searchType As SearchType, _
                       ByVal queryFields() As String) As QueryCmd
            Return From(New SearchFragment(searchString, searchType, queryFields))
        End Function

        Public Function FromSearch(ByVal searchString As String, ByVal ParamArray queryFields() As String) As QueryCmd
            Return From(New SearchFragment(searchString, queryFields))
        End Function

#End Region

        Public Function Union(ByVal q As QueryCmd) As QueryCmd
            Unions = New ObjectModel.ReadOnlyCollection(Of Pair(Of Boolean, QueryCmd))( _
                New Pair(Of Boolean, QueryCmd)() {New Pair(Of Boolean, QueryCmd)(False, q)})
            Return Me
        End Function

        Public Function UnionAll(ByVal q As QueryCmd) As QueryCmd
            Unions = New ObjectModel.ReadOnlyCollection(Of Pair(Of Boolean, QueryCmd))( _
                New Pair(Of Boolean, QueryCmd)() {New Pair(Of Boolean, QueryCmd)(True, q)})
            Return Me
        End Function

        Public Function SelectEntity(ByVal ParamArray t() As EntityUnion) As QueryCmd
            SelectedEntities = New ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))( _
                Array.ConvertAll(Of EntityUnion, Pair(Of EntityUnion, Boolean?))(t, _
                    Function(item As EntityUnion) New Pair(Of EntityUnion, Boolean?)(item, Nothing)))
            Return Me
        End Function

        Public Function SelectEntity(ByVal ParamArray t() As Type) As QueryCmd
            SelectedEntities = New ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))( _
                Array.ConvertAll(Of Type, Pair(Of EntityUnion, Boolean?))(t, _
                    Function(item As Type) New Pair(Of EntityUnion, Boolean?)(New EntityUnion(item), Nothing)))
            Return Me
        End Function

        Public Function SelectEntity(ByVal ParamArray entityNames() As String) As QueryCmd
            SelectedEntities = New ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))( _
                Array.ConvertAll(Of String, Pair(Of EntityUnion, Boolean?))(entityNames, _
                    Function(item As String) New Pair(Of EntityUnion, Boolean?)(New EntityUnion(item), Nothing)))
            Return Me
        End Function

        Public Function SelectEntity(ByVal ParamArray aliases() As QueryAlias) As QueryCmd
            SelectedEntities = New ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))( _
                Array.ConvertAll(Of QueryAlias, Pair(Of EntityUnion, Boolean?))(aliases, _
                    Function(item As QueryAlias) New Pair(Of EntityUnion, Boolean?)(New EntityUnion(item), Nothing)))
            Return Me
        End Function

        Public Function [Select](ByVal ParamArray props() As ObjectProperty) As QueryCmd
            Dim f As New FCtor.Int
            For Each p As ObjectProperty In props
                f = f.prop(p)
            Next
            Return [Select](f)
        End Function

        Public Function [Select](ByVal ParamArray exp() As ECtor.Int) As QueryCmd
            Dim f As New List(Of SelectExpression)
            For Each ei As ECtor.Int In exp
                For Each e As IGetExpression In ei.AsEnumerable
                    If TypeOf e Is SelectExpression Then
                        f.Add(CType(e, SelectExpression))
                    Else
                        f.Add(New SelectExpression(e.Expression))
                    End If
                Next
            Next
            Return [Select](New ObjectModel.ReadOnlyCollection(Of SelectExpression)(f))
        End Function

        Public Function [Select](ByVal fields() As SelectExpression) As QueryCmd
            SelectList = New ObjectModel.ReadOnlyCollection(Of SelectExpression)(fields)
            Return Me
        End Function

        Public Function [Select](ByVal fields As ObjectModel.ReadOnlyCollection(Of SelectExpression)) As QueryCmd
            SelectList = fields
            Return Me
        End Function

        Public Function [Select](ByVal fields As IList(Of SelectExpression)) As QueryCmd
            SelectList = New ObjectModel.ReadOnlyCollection(Of SelectExpression)(fields)
            Return Me
        End Function

        Friend Sub SelectInt(ByVal t As Type, ByVal mpe As ObjectMappingEngine)
            If _filter IsNot Nothing Then
                For Each f As IFilter In _filter.Filter.GetAllFilters
                    Dim ef As IEntityFilter = TryCast(f, IEntityFilter)
                    If ef IsNot Nothing Then
                        Dim rt As Type = CType(ef.Template, OrmFilterTemplate).ObjectSource.GetRealType(mpe)
                        If t.IsAssignableFrom(rt) Then
                            _sel = New SelectClauseDef(New List(Of Pair(Of EntityUnion, Boolean?))(New Pair(Of EntityUnion, Boolean?)() {New Pair(Of EntityUnion, Boolean?)(New EntityUnion(rt), Nothing)}))
#If nlog Then
                            'NLog.LogManager.GetCurrentClassLogger?.Trace("_cmd._sel filter {0}", Environment.StackTrace)
#End If
                            Return
                        End If
                    End If
                Next
            End If

            _sel = New SelectClauseDef(New List(Of Pair(Of EntityUnion, Boolean?))(New Pair(Of EntityUnion, Boolean?)() {New Pair(Of EntityUnion, Boolean?)(New EntityUnion(t), Nothing)}))
#If nlog Then
            'NLog.LogManager.GetCurrentClassLogger?.Trace("_cmd._sel int {0}", Environment.StackTrace)
#End If
        End Sub

        Public Function SelectEntity(ByVal eu As EntityUnion, ByVal withLoad As Boolean) As QueryCmd
            Dim l As New List(Of Pair(Of EntityUnion, Boolean?))(New Pair(Of EntityUnion, Boolean?)() {New Pair(Of EntityUnion, Boolean?)(eu, withLoad)})
            SelectedEntities = New ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))(l)
            Return Me
        End Function

        Public Function SelectEntity(ByVal t As Type, ByVal withLoad As Boolean) As QueryCmd
            Dim l As New List(Of Pair(Of EntityUnion, Boolean?))(New Pair(Of EntityUnion, Boolean?)() {New Pair(Of EntityUnion, Boolean?)(New EntityUnion(t), withLoad)})
            SelectedEntities = New ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))(l)
            Return Me
        End Function

        Public Function SelectEntity(ByVal entityName As String, ByVal withLoad As Boolean) As QueryCmd
            Dim l As New List(Of Pair(Of EntityUnion, Boolean?))(New Pair(Of EntityUnion, Boolean?)() {New Pair(Of EntityUnion, Boolean?)(New EntityUnion(entityName), withLoad)})
            SelectedEntities = New ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))(l)
            Return Me
        End Function

        Public Function SelectEntity(ByVal [alias] As QueryAlias, ByVal withLoad As Boolean) As QueryCmd
            Dim l As New List(Of Pair(Of EntityUnion, Boolean?))(New Pair(Of EntityUnion, Boolean?)() {New Pair(Of EntityUnion, Boolean?)(New EntityUnion([alias]), withLoad)})
            SelectedEntities = New ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))(l)
            Return Me
        End Function

        Public Function AddEntityToSelectList(ByVal eu As EntityUnion, ByVal withLoad As Boolean?) As QueryCmd
            Dim l As New List(Of Pair(Of EntityUnion, Boolean?))()
            If SelectedEntities IsNot Nothing Then
                l.AddRange(SelectedEntities)
            End If
            l.Add(New Pair(Of EntityUnion, Boolean?)(eu, withLoad))
            SelectedEntities = New ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))(l)
            Return Me
        End Function

        Public Function AddEntityToSelectList(ByVal t As Type, ByVal withLoad As Boolean) As QueryCmd
            Dim l As New List(Of Pair(Of EntityUnion, Boolean?))()
            If SelectedEntities IsNot Nothing Then
                l.AddRange(SelectedEntities)
            End If
            l.AddRange(New Pair(Of EntityUnion, Boolean?)() {New Pair(Of EntityUnion, Boolean?)(New EntityUnion(t), withLoad)})
            SelectedEntities = New ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))(l)
            Return Me
        End Function

        Public Function AddEntityToSelectList(ByVal entityName As String, ByVal withLoad As Boolean) As QueryCmd
            Dim l As New List(Of Pair(Of EntityUnion, Boolean?))()
            If SelectedEntities IsNot Nothing Then
                l.AddRange(SelectedEntities)
            End If
            l.AddRange(New Pair(Of EntityUnion, Boolean?)() {New Pair(Of EntityUnion, Boolean?)(New EntityUnion(entityName), withLoad)})
            SelectedEntities = New ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))(l)
            Return Me
        End Function

        Public Function AddEntityToSelectList(ByVal [alias] As QueryAlias, ByVal withLoad As Boolean) As QueryCmd
            Dim l As New List(Of Pair(Of EntityUnion, Boolean?))()
            If SelectedEntities IsNot Nothing Then
                l.AddRange(SelectedEntities)
            End If
            l.AddRange(New Pair(Of EntityUnion, Boolean?)() {New Pair(Of EntityUnion, Boolean?)(New EntityUnion([alias]), withLoad)})
            SelectedEntities = New ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))(l)
            Return Me
        End Function

        Public Function AddToSelectList(ByVal ParamArray exp() As SelectExpression) As QueryCmd
            Dim l As New List(Of SelectExpression)()
            If SelectList IsNot Nothing Then
                l.AddRange(SelectList)
            End If
            l.AddRange(exp)
            SelectList = New ObjectModel.ReadOnlyCollection(Of SelectExpression)(l)
            Return Me
        End Function

        Public Function GroupBy(ByVal group As GroupExpression) As QueryCmd
            Me.Group = group
            Return Me
        End Function

        Public Function Paging(ByVal start As Integer, ByVal length As Integer) As QueryCmd
            ClientPaging = New Paging(start, length)
            Return Me
        End Function

        Public Function Paging(ByVal pager As IPager) As QueryCmd
            Me.Pager = pager
            Return Me
        End Function

        Public Function Skip(ByVal skipedPosition As Integer) As QueryCmd
            If skipedPosition <= 0 Then
                Throw New ArgumentException("Параметр должен быть больше нуля", "skipedPosition")
            End If

            Dim filterInfo As RowNumberFilterInfo = New RowNumberFilterInfo(RowNumberFilter)

            Select Case filterInfo.Status
                Case RowNumberFilterInfo.RowNumberFilterStatus.IsClearFilter
                    Me.RowNumberFilter = New TableFilter( _
                        QueryCmd.RowNumerColumn, _
                        New ScalarValue(skipedPosition), _
                        Criteria.FilterOperation.GreaterThan)
                Case RowNumberFilterInfo.RowNumberFilterStatus.IsSkip
                    Me.RowNumberFilter = New TableFilter( _
                        QueryCmd.RowNumerColumn, _
                        New ScalarValue(filterInfo.FromPostion + skipedPosition), _
                        Criteria.FilterOperation.GreaterThan)
                Case RowNumberFilterInfo.RowNumberFilterStatus.IsTake
                    Me.RowNumberFilter = New TableFilter( _
                        QueryCmd.RowNumerColumn, _
                        New BetweenValue(skipedPosition + 1, filterInfo.ToPostion), _
                        Criteria.FilterOperation.Between)
                Case RowNumberFilterInfo.RowNumberFilterStatus.IsSkipTake
                    Me.RowNumberFilter = New TableFilter( _
                        QueryCmd.RowNumerColumn, _
                        New BetweenValue(skipedPosition + filterInfo.FromPostion, filterInfo.ToPostion), _
                        Criteria.FilterOperation.Between)
                Case Else
                    Throw New NotImplementedException(filterInfo.Status.ToString)
            End Select

            Return Me
        End Function

        Public Function Take(ByVal takedPosition As Integer) As QueryCmd
            If takedPosition <= 0 Then
                Throw New ArgumentException("Параметр должен быть больше нуля", "takedPosition")
            End If

            Dim filterInfo As RowNumberFilterInfo = New RowNumberFilterInfo(RowNumberFilter)

            Select Case filterInfo.Status
                Case RowNumberFilterInfo.RowNumberFilterStatus.IsClearFilter
                    Me.RowNumberFilter = New TableFilter( _
                        QueryCmd.RowNumerColumn, _
                        New ScalarValue(takedPosition), _
                        Criteria.FilterOperation.LessEqualThan)
                Case RowNumberFilterInfo.RowNumberFilterStatus.IsSkip
                    Me.RowNumberFilter = New TableFilter( _
                        QueryCmd.RowNumerColumn, _
                        New BetweenValue(filterInfo.FromPostion + 1, filterInfo.FromPostion + takedPosition), _
                        Criteria.FilterOperation.Between)
                Case RowNumberFilterInfo.RowNumberFilterStatus.IsTake
                    Me.RowNumberFilter = New TableFilter( _
                        QueryCmd.RowNumerColumn, _
                        New ScalarValue(Math.Min(filterInfo.ToPostion, takedPosition)), _
                        Criteria.FilterOperation.LessEqualThan)
                Case RowNumberFilterInfo.RowNumberFilterStatus.IsSkipTake
                    Me.RowNumberFilter = New TableFilter( _
                        QueryCmd.RowNumerColumn, _
                        New BetweenValue( _
                                filterInfo.FromPostion, _
                                filterInfo.FromPostion + Math.Min(filterInfo.ToPostion - filterInfo.FromPostion + 1, takedPosition) - 1), _
                        Criteria.FilterOperation.Between)
                Case Else
                    Throw New NotImplementedException(filterInfo.Status.ToString)
            End Select

            'If Not Me.RowNumberFilter Is Nothing Then
            '    Dim prevValue As ScalarValue = TryCast(Me.RowNumberFilter.Value, ScalarValue)
            '    If prevValue Is Nothing Then
            '        Me.RowNumberFilter = New TableFilter(QueryCmd.RowNumerColumn, New ScalarValue(takedPosition), Criteria.FilterOperation.LessEqualThan)
            '    Else
            '        Dim toPosition As Int32 = takedPosition + CType(prevValue.Value, Int32)
            '        Dim fromPosition As Int32 = CType(prevValue.Value, Int32) + 1
            '        Me.RowNumberFilter = New TableFilter(QueryCmd.RowNumerColumn, New BetweenValue(fromPosition, toPosition), Criteria.FilterOperation.Between)
            '    End If
            'Else
            '    Me.RowNumberFilter = New TableFilter(QueryCmd.RowNumerColumn, New ScalarValue(takedPosition), Criteria.FilterOperation.LessEqualThan)
            'End If

            Return Me
        End Function


        'Public Function [SelectAgg](ByVal aggrs() As AggregateBase) As QueryCmd
        '    Aggregates = New ObjectModel.ReadOnlyCollection(Of AggregateBase)(aggrs)
        '    Return Me
        'End Function

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

        Protected Friend Function GetManager() As IGetManager
            Dim mgr As OrmManager = OrmManager.CurrentManager
            If mgr Is Nothing Then
                If _getMgr IsNot Nothing Then
                    Return New GetManagerDisposable(_getMgr.CreateManager(Me), _schema)
                Else
                    Return Nothing
                End If
            Else
                'don't dispose
                Return New ManagerWrapper(mgr, mgr.MappingEngine)
            End If
        End Function
        Protected Function GetManager(cm As ICreateManager) As IGetManager
            Dim mgr As OrmManager = OrmManager.CurrentManager
            If mgr Is Nothing Then
                If cm IsNot Nothing Then
                    Return New GetManagerDisposable(cm.CreateManager(Me), _schema)
                Else
                    Return Nothing
                End If
            Else
                'don't dispose
                Return New ManagerWrapper(mgr, mgr.MappingEngine)
            End If
        End Function

        Protected Function GetManager(cm As CreateManagerDelegate) As IGetManager
            Dim mgr As OrmManager = OrmManager.CurrentManager
            If mgr Is Nothing Then
                If cm IsNot Nothing Then
                    Return New GetManagerDisposable(cm(), _schema)
                Else
                    Return Nothing
                End If
            Else
                'don't dispose
                Return New ManagerWrapper(mgr, mgr.MappingEngine)
            End If
        End Function

#Region " ToLists "

        Public Function ToMatrix() As ReadonlyMatrix
            Return ToMatrix(_getMgr)
        End Function

        Public Function ToMatrix(ByVal getMgr As ICreateManager) As ReadonlyMatrix
            Using gm = GetManager(getMgr)
                If gm Is Nothing Then
                    Throw New QueryCmdException("OrmManager required", Me)
                End If
                Using New SetManagerHelper(gm.Manager, getMgr, _schema, ContextInfo)
                    Return ToMatrix(gm.Manager)
                End Using
            End Using
        End Function

        Public Function ToMatrix(ByVal mgr As OrmManager) As ReadonlyMatrix
            If mgr Is Nothing Then
                Throw New ArgumentNullException("mgr")
            End If

            Return GetExecutor(mgr).Exec(mgr, Me)
        End Function

#Region " ToList "
        Public Function ToBaseEntity(Of T As _IEntity)(ByVal getMgr As ICreateManager, ByVal withLoad As Boolean) As IList(Of T)
            Using gm = GetManager(getMgr)
                If gm Is Nothing Then
                    Throw New QueryCmdException("OrmManager required", Me)
                End If

                Using New SetManagerHelper(gm.Manager, getMgr, _schema, ContextInfo)
                    Return ToBaseEntity(Of T)(gm.Manager, withLoad)
                End Using
            End Using
        End Function

        Public Function ToBaseEntity(Of T As _IEntity)(ByVal withLoad As Boolean) As IList(Of T)
            Return ToBaseEntity(Of T)(_getMgr, withLoad)
        End Function

        Public Function ToBaseEntity(Of T As _IEntity)(ByVal mgr As OrmManager) As IList(Of T)
            Return ToBaseEntity(Of T)(mgr, False)
        End Function

        Public Function ToBaseEntity(Of T As _IEntity)(ByVal mgr As OrmManager, ByVal withLoad As Boolean) As IList(Of T)
            If mgr Is Nothing Then
                Throw New ArgumentNullException("mgr")
            End If

            If SelectList IsNot Nothing Then
                Throw New NotSupportedException("Multi types")
            Else
                Dim selOS As EntityUnion = GetSelectedOS()
                If selOS Is Nothing Then
                    selOS = New EntityUnion(GetType(T))
                ElseIf SelectedEntities IsNot Nothing Then
                    If SelectedEntities.Count > 1 OrElse SelectedEntities(0).First IsNot selOS Then
                        Throw New NotSupportedException("Multi types")
                    End If
                End If

                Dim oldjs() As QueryJoin = _joins
                Dim sel As SelectClauseDef = SelectClause
                Dim oldF As FromClauseDef = FromClause

                If oldF Is Nothing Then
                    From(selOS)
                End If

                Try
                    SelectClause = Nothing
                    Dim st As Type = selOS.GetRealType(mgr.MappingEngine)
                    Dim spk As String = mgr.MappingEngine.GetPrimaryKey(st)
                    Dim types As ICollection(Of Type) = mgr.MappingEngine.GetDerivedTypes(st)
                    For Each tt As Type In types
                        Dim pk As String = mgr.MappingEngine.GetPrimaryKey(tt)
                        Join(JCtor.left_join(tt).on(selOS, spk).eq(tt, pk))
                        AddEntityToSelectList(tt, withLoad)
                    Next
                    Dim l As New List(Of T)
                    For Each row As System.Collections.ObjectModel.ReadOnlyCollection(Of _IEntity) In ToMatrix()
                        For i As Integer = 0 To types.Count - 1
                            If row(i) IsNot Nothing Then
                                l.Add(CType(row(i), T))
                                Exit For
                            End If
                        Next
                    Next
                    Return l
                Finally
                    _sel = sel
                    _joins = oldjs
                    If oldF Is Nothing Then
                        FromClause = Nothing
                    End If
                End Try
            End If

        End Function

        Public Function ToList(ByVal mgr As OrmManager) As IList
            If mgr Is Nothing Then
                Throw New ArgumentNullException("mgr")
            End If

            Dim st As Type = Nothing
            If CreateType IsNot Nothing Then
                st = CreateType.GetRealType(mgr.MappingEngine)
            Else
                st = GetSelectedType(mgr.MappingEngine)
            End If

            Dim t As MethodInfo = Nothing
            If GetType(AnonymousEntity).IsAssignableFrom(st) Then
                Return ToAnonymList(mgr)
            ElseIf GetType(_ICachedEntity).IsAssignableFrom(st) Then
                If st.IsAbstract Then
                    t = Me.GetType.GetMethod("ToBaseEntity", New Type() {GetType(OrmManager)})
                Else
                    t = Me.GetType.GetMethod("ToEntityList", New Type() {GetType(OrmManager)})
                End If
            ElseIf GetType(_IEntity).IsAssignableFrom(st) Then
                t = Me.GetType.GetMethod("ToObjectList", New Type() {GetType(OrmManager)})
            Else
                t = Me.GetType.GetMethod("ToPOCOList", New Type() {GetType(OrmManager)})
            End If
            t = t.MakeGenericMethod(New Type() {st})

            Try
                Return CType(t.Invoke(Me, New Object() {mgr}), System.Collections.IList)
            Catch ex As TargetInvocationException
                CoreFramework.CFDebugging.Stack.PreserveStackTrace(ex.InnerException)
                Throw ex.InnerException
            End Try
        End Function

        Public Function ToList(ByVal getMgr As ICreateManager) As IList
            Using gm = GetManager(getMgr)
                If gm Is Nothing Then
                    Throw New QueryCmdException("OrmManager required", Me)
                End If

                Using New SetManagerHelper(gm.Manager, getMgr, _schema, ContextInfo)
                    Return ToList(gm.Manager)
                End Using
            End Using
        End Function

        Public Function ToList() As IList
            Return ToList(_getMgr)
        End Function

        Public Function ToList(Of CreateType As {New, _ICachedEntity}, ReturnType As _ICachedEntity)(ByVal mgr As OrmManager) As ReadOnlyEntityList(Of ReturnType)
            If mgr Is Nothing Then
                Throw New ArgumentNullException("mgr")
            End If

            Return GetExecutor(mgr).Exec(Of CreateType, ReturnType)(mgr, Me)
        End Function

        Public Function ToList(Of CreateType As {New, _ICachedEntity}, ReturnType As _ICachedEntity)(ByVal getMgr As ICreateManager) As ReadOnlyEntityList(Of ReturnType)
            Using gm = GetManager(getMgr)
                If gm Is Nothing Then
                    Throw New QueryCmdException("OrmManager required", Me)
                End If

                Using New SetManagerHelper(gm.Manager, getMgr, _schema, ContextInfo)
                    Return ToList(Of CreateType, ReturnType)(gm.Manager)
                End Using
            End Using
        End Function

        Public Function ToList(Of CreateType As {New, _ICachedEntity}, ReturnType As _ICachedEntity)() As ReadOnlyEntityList(Of ReturnType)
            Return ToList(Of CreateType, ReturnType)(_getMgr)
        End Function

        Public Function ToList(Of CreateReturnType As {New, _ICachedEntity})(ByVal mgr As OrmManager) As ReadOnlyEntityList(Of CreateReturnType)
            If mgr Is Nothing Then
                Throw New ArgumentNullException("mgr")
            End If

            Return GetExecutor(mgr).Exec(Of CreateReturnType, CreateReturnType)(mgr, Me)
        End Function

        Public Function ToList(Of CreateReturnType As {New, _ICachedEntity})(ByVal getMgr As ICreateManager) As ReadOnlyEntityList(Of CreateReturnType)
            Using gm = GetManager(getMgr)
                If gm Is Nothing Then
                    Throw New QueryCmdException("OrmManager required", Me)
                End If

                Using New SetManagerHelper(gm.Manager, getMgr, _schema, ContextInfo)
                    Return ToList(Of CreateReturnType)(gm.Manager)
                End Using
            End Using
        End Function
        Public Function ToList(Of CreateReturnType As {New, _ICachedEntity})(ByVal getMgr As CreateManagerDelegate) As ReadOnlyEntityList(Of CreateReturnType)
            Using gm = GetManager(getMgr)
                If gm Is Nothing Then
                    Throw New QueryCmdException("OrmManager required", Me)
                End If

                Using New SetManagerHelper(gm.Manager, getMgr, _schema, ContextInfo)
                    Return ToList(Of CreateReturnType)(gm.Manager)
                End Using
            End Using
        End Function
        Public Function ToList(Of CreateReturnType As {New, _ICachedEntity})() As ReadOnlyEntityList(Of CreateReturnType)
            Return ToList(Of CreateReturnType)(_getMgr)
        End Function
#End Region

#Region " ToAnonymList "

        Public Function ToAnonymList(ByVal mgr As OrmManager) As ReadOnlyObjectList(Of AnonymousEntity)
            If mgr Is Nothing Then
                Throw New ArgumentNullException("mgr")
            End If
            'Dim c As New svct(Me)
            '_createType = GetType(AnonymousEntity)
            'Using New OnExitScopeAction(AddressOf c.SetCT2Nothing)
            Return GetExecutor(mgr).ExecEntity(Of AnonymousEntity, AnonymousEntity)(mgr, Me)
            'End Using
        End Function

        Public Function ToAnonymList(ByVal getMgr As ICreateManager) As ReadOnlyObjectList(Of AnonymousEntity)
            Using gm = GetManager(getMgr)
                If gm Is Nothing Then
                    Throw New QueryCmdException("OrmManager required", Me)
                End If

                Using New SetManagerHelper(gm.Manager, getMgr, _schema, ContextInfo)
                    Return ToAnonymList(gm.Manager)
                End Using
            End Using
        End Function

        Public Function ToAnonymList() As ReadOnlyObjectList(Of AnonymousEntity)
            Return ToAnonymList(_getMgr)
        End Function

#End Region

#Region " ToEntityList "

        Public Function ToEntityList(Of T As ICachedEntity)(ByVal getMgr As CreateManagerDelegate) As ReadOnlyEntityList(Of T)
            Using gm = GetManager(getMgr)
                If gm Is Nothing Then
                    Throw New QueryCmdException("OrmManager required", Me)
                End If

                Using New SetManagerHelper(gm.Manager, getMgr, _schema, ContextInfo)
                    Return ToEntityList(Of T)(gm.Manager)
                End Using
            End Using
        End Function

        Public Function ToEntityList(Of T As ICachedEntity)(ByVal getMgr As ICreateManager) As ReadOnlyEntityList(Of T)
            Using gm = GetManager(getMgr)
                If gm Is Nothing Then
                    Throw New QueryCmdException("OrmManager required", Me)
                End If

                Using New SetManagerHelper(gm.Manager, getMgr, _schema, ContextInfo)
                    Return ToEntityList(Of T)(gm.Manager)
                End Using
            End Using
        End Function

        Public Function ToEntityList(Of T As ICachedEntity)(ByVal mgr As OrmManager) As ReadOnlyEntityList(Of T)
            If mgr Is Nothing Then
                Throw New ArgumentNullException("mgr")
            End If

            If GetType(AnonymousCachedEntity).IsAssignableFrom(GetType(T)) AndAlso CreateType Is Nothing Then
                Return GetExecutor(mgr).Exec(Of AnonymousCachedEntity, T)(mgr, Me)
            Else
                Return GetExecutor(mgr).Exec(Of T)(mgr, Me)
            End If
        End Function

        Public Function ToEntityList(Of T As ICachedEntity)() As ReadOnlyEntityList(Of T)
            Return ToEntityList(Of T)(_getMgr)
        End Function

#End Region

#Region " ToOrmList "

        Public Function ToOrmListDyn(Of T As {_ISinglePKEntity})(ByVal mgr As OrmManager) As ReadOnlyList(Of T)
            If mgr Is Nothing Then
                Throw New ArgumentNullException("mgr")
            End If

            Return CType(ToEntityList(Of T)(mgr), ReadOnlyList(Of T))
        End Function

        Public Function ToOrmListDyn(Of T As {_ISinglePKEntity})(ByVal getMgr As ICreateManager) As ReadOnlyList(Of T)
            'Using gm = GetManager(getMgr)
            '    mgr.RaiseObjectCreation = True
            '    AddHandler mgr.ObjectCreated, AddressOf New cls(getMgr).ObjectCreated
            '    Return ToOrmList(Of T)(mgr)
            'End Using
            Return CType(ToEntityList(Of T)(getMgr), ReadOnlyList(Of T))
        End Function

        Public Function ToOrmListDyn(Of T As {_ISinglePKEntity})(ByVal getMgr As CreateManagerDelegate) As ReadOnlyList(Of T)
            Using gm = GetManager(getMgr)
                If gm Is Nothing Then
                    Throw New QueryCmdException("OrmManager required", Me)
                End If

                Using New SetManagerHelper(gm.Manager, getMgr, _schema, ContextInfo)
                    Return ToOrmListDyn(Of T)(gm.Manager)
                End Using
            End Using
        End Function

        Public Function ToOrmListDyn(Of T As _ISinglePKEntity)() As ReadOnlyList(Of T)
            Return ToOrmListDyn(Of T)(_getMgr)
        End Function

        Public Function ToOrmList(Of CreateType As {New, _ISinglePKEntity}, ReturnType As _ISinglePKEntity)(ByVal mgr As OrmManager) As ReadOnlyList(Of ReturnType)
            If mgr Is Nothing Then
                Throw New ArgumentNullException("mgr")
            End If

            Return CType(ToList(Of CreateType, ReturnType)(mgr), ReadOnlyList(Of ReturnType))
        End Function

        Public Function ToOrmList(Of CreateType As {New, _ISinglePKEntity}, ReturnType As _ISinglePKEntity)(ByVal getMgr As ICreateManager) As ReadOnlyList(Of ReturnType)
            'Using gm = GetManager(getMgr)
            '    mgr.RaiseObjectCreation = True
            '    AddHandler mgr.ObjectCreated, AddressOf New cls(getMgr).ObjectCreated
            '    Return ToOrmList(Of CreateType, ReturnType)(mgr)
            'End Using
            Return CType(ToList(Of CreateType, ReturnType)(getMgr), ReadOnlyList(Of ReturnType))
        End Function

        Public Function ToOrmList(Of CreateReturnType As {New, _ISinglePKEntity})(ByVal mgr As OrmManager) As ReadOnlyList(Of CreateReturnType)
            If mgr Is Nothing Then
                Throw New ArgumentNullException("mgr")
            End If

            Return CType(ToList(Of CreateReturnType)(mgr), ReadOnlyList(Of CreateReturnType))
        End Function

        Public Function ToOrmList(Of CreateReturnType As {New, _ISinglePKEntity})(ByVal getMgr As ICreateManager) As ReadOnlyList(Of CreateReturnType)
            Return CType(ToList(Of CreateReturnType)(getMgr), ReadOnlyList(Of CreateReturnType))
        End Function

        Public Function ToOrmList(Of CreateReturnType As {New, _ISinglePKEntity})() As ReadOnlyList(Of CreateReturnType)
            Return CType(ToList(Of CreateReturnType)(), ReadOnlyList(Of CreateReturnType))
        End Function
#End Region

#Region " ToSimpleList "

        Public Function ToSimpleList(Of T)(ByVal mgr As OrmManager) As IList(Of T)
            If mgr Is Nothing Then
                Throw New ArgumentNullException("mgr")
            End If

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
            Using gm = GetManager(getMgr)
                If gm Is Nothing Then
                    Throw New QueryCmdException("OrmManager required", Me)
                End If

                Using New SetManagerHelper(gm.Manager, getMgr, _schema, ContextInfo)
                    Return GetExecutor(gm.Manager).ExecSimple(Of T)(gm.Manager, Me)
                End Using
            End Using
        End Function

        Public Function ToSimpleList(Of T)() As IList(Of T)
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
        '    Using gm = GetManager(getMgr)
        '        'mgr.RaiseObjectCreation = True
        '        AddHandler mgr.ObjectLoaded, AddressOf New cls(getMgr).ObjectCreated
        '        Return GetExecutor(mgr).ExecSimple(Of CreateType, T)(mgr, Me)
        '    End Using
        'End Function

        'Public Function ToSimpleList(Of CreateType As {New, _ICachedEntity}, T)() As IList(Of T)
        '    If _getMgr Is Nothing Then
        '        Throw New QueryCmdException("OrmManager required", me)
        '    End If

        '    Return ToSimpleList(Of CreateType, T)(_getMgr)
        'End Function
#End Region

#Region " Count "

        Public Overridable Function Count(ByVal mgr As OrmManager) As Integer
            If mgr Is Nothing Then
                Throw New ArgumentNullException("mgr")
            End If

            Dim s As OrderByClause = _order
            Dim p As Paging = _clientPage
            Dim pp As IPager = _pager
            Dim rf As TableFilter = _rn
            Try
                _order = Nothing
                _clientPage = Nothing
                _pager = Nothing
                _rn = Nothing
                Dim c As New QueryCmd.svct(Me)
                Using New OnExitScopeAction(AddressOf c.SetCT2Nothing)
                    If FromClause Is Nothing Then
                        If SelectedEntities Is Nothing Then
                            Throw New QueryCmdException("Neither FromClause nor SelectTypes not specified", Me)
                        End If
                        From(SelectedEntities(0).First)
                    End If
                    Return [Select](FCtor.count).SingleOrDefaultSimple(Of Integer)(mgr)
                End Using
            Finally
                Sort = s
                _clientPage = p
                _pager = pp
                _rn = rf
            End Try
        End Function

        Public Function Count(ByVal getMgr As ICreateManager) As Integer
            Using gm = GetManager(getMgr)
                If gm Is Nothing Then
                    Throw New QueryCmdException("OrmManager required", Me)
                End If

                Using New SetManagerHelper(gm.Manager, getMgr, _schema, ContextInfo)
                    Return Count(gm.Manager)
                End Using
            End Using
        End Function

        Public Function Count() As Integer
            Return Count(_getMgr)
        End Function

#End Region

        Public Function ToDictionary(Of TKey As ICachedEntity, TValue As ICachedEntity)(ByVal mgr As OrmManager) As IDictionary(Of TKey, IList(Of TValue))
            If mgr Is Nothing Then
                Throw New ArgumentNullException("mgr")
            End If

            Dim c As New QueryCmd.svct(Me)
            Using New OnExitScopeAction(AddressOf c.SetCT2Nothing)
                If SelectClause Is Nothing Then
                    SelectEntity(GetType(TKey), GetType(TValue))
                End If

                Dim m As ReadonlyMatrix = ToMatrix(mgr)

                Dim d As New Dictionary(Of TKey, IList(Of TValue))

                For Each row As ReadOnlyCollection(Of _IEntity) In m
                    Dim l As IList(Of TValue) = Nothing
                    Dim key As TKey = CType(row(0), TKey)
                    Dim val As TValue = CType(row(1), TValue)
                    If Not d.TryGetValue(key, l) Then
                        l = New List(Of TValue)
                        d(key) = l
                    End If
                    If val IsNot Nothing Then
                        l.Add(val)
                    End If
                Next

                Return d
            End Using
        End Function

        Public Function ToDictionary(Of TKey As ICachedEntity, TValue As ICachedEntity)() As IDictionary(Of TKey, IList(Of TValue))
            Return ToDictionary(Of TKey, TValue)(_getMgr)
        End Function

        Public Function ToDictionary(Of TKey As ICachedEntity, TValue As ICachedEntity)(ByVal getMgr As ICreateManager) As IDictionary(Of TKey, IList(Of TValue))
            Using gm = GetManager(getMgr)
                If gm Is Nothing Then
                    Throw New QueryCmdException("OrmManager required", Me)
                End If

                Using New SetManagerHelper(gm.Manager, getMgr, _schema, ContextInfo)
                    Return ToDictionary(Of TKey, TValue)(gm.Manager)
                End Using
            End Using
        End Function

        Public Function ToSimpleDictionary(Of TKey, TValue)(ByVal mgr As OrmManager) As IDictionary(Of TKey, IList(Of TValue))
            If mgr Is Nothing Then
                Throw New ArgumentNullException("mgr")
            End If

            Dim m As ReadonlyMatrix = ToMatrix(mgr)

            Dim d As New Dictionary(Of TKey, IList(Of TValue))

            For Each row As ReadOnlyCollection(Of _IEntity) In m
                Dim l As IList(Of TValue) = Nothing
                Dim key As TKey = CType(row(0), TKey)
                Dim val As TValue = CType(row(1), TValue)
                If Not d.TryGetValue(key, l) Then
                    l = New List(Of TValue)
                    d(key) = l
                End If
                l.Add(val)
            Next

            Return d
        End Function

        Public Function ToSimpleDictionary(Of TKey, TValue)() As IDictionary(Of TKey, IList(Of TValue))
            Return ToSimpleDictionary(Of TKey, TValue)(_getMgr)
        End Function

        Public Function ToSimpleDictionary(Of TKey, TValue)(ByVal getMgr As ICreateManager) As IDictionary(Of TKey, IList(Of TValue))
            Using gm = GetManager(getMgr)
                If gm Is Nothing Then
                    Throw New QueryCmdException("OrmManager required", Me)
                End If

                Using New SetManagerHelper(gm.Manager, getMgr, _schema, ContextInfo)
                    Return ToSimpleDictionary(Of TKey, TValue)(gm.Manager)
                End Using
            End Using
        End Function

        Public Function ToObjectList(Of T As _IEntity)() As ReadOnlyObjectList(Of T)
            Return ToObjectList(Of T)(_getMgr)
        End Function

        Public Function ToObjectList(Of T As _IEntity)(ByVal getMgr As ICreateManager) As ReadOnlyObjectList(Of T)
            Using gm = GetManager(getMgr)
                If gm Is Nothing Then
                    Throw New QueryCmdException("OrmManager required", Me)
                End If

                Using New SetManagerHelper(gm.Manager, getMgr, _schema, ContextInfo)
                    Return ToObjectList(Of T)(gm.Manager)
                End Using

            End Using
        End Function
        Public Function ToObjectList(Of T As _IEntity)(ByVal getMgr As CreateManagerDelegate) As ReadOnlyObjectList(Of T)
            Using gm = GetManager(getMgr)
                If gm Is Nothing Then
                    Throw New QueryCmdException("OrmManager required", Me)
                End If

                Using New SetManagerHelper(gm.Manager, getMgr, _schema, ContextInfo)
                    Return ToObjectList(Of T)(gm.Manager)
                End Using

            End Using
        End Function
        Public Function ToObjectList(Of T As _IEntity)(ByVal mgr As OrmManager) As ReadOnlyObjectList(Of T)
            If mgr Is Nothing Then
                Throw New ArgumentNullException("mgr")
            End If

            If GetType(AnonymousCachedEntity).IsAssignableFrom(GetType(T)) AndAlso CreateType Is Nothing Then
                Return GetExecutor(mgr).ExecEntity(Of AnonymousCachedEntity, T)(mgr, Me)
            ElseIf GetType(AnonymousEntity).IsAssignableFrom(GetType(T)) AndAlso CreateType Is Nothing Then
                Return GetExecutor(mgr).ExecEntity(Of AnonymousEntity, T)(mgr, Me)
            ElseIf GetType(CachedEntity).IsAssignableFrom(GetType(T)) Then
                Return CType(ToList(mgr), Global.Worm.ReadOnlyObjectList(Of T))
            Else
                Return GetExecutor(mgr).ExecEntity(Of T)(mgr, Me)
            End If
        End Function

        Public Function ToPOCOList(Of T As {New, Class})() As IList(Of T)
            Using gm = GetManager(_getMgr)
                If gm Is Nothing Then
                    Throw New QueryCmdException("OrmManager required", Me)
                End If

                Using New SetManagerHelper(gm.Manager, CreateManager, _schema, ContextInfo)
                    Return ToPOCOList(Of T)(gm.Manager)
                End Using
            End Using
        End Function

        Friend Sub AddPOCO(ByVal rt As Type, ByVal selSchema As IEntitySchema)
            If _poco Is Nothing Then
                _poco = Hashtable.Synchronized(New Hashtable)
            End If

            If Not _poco.Contains(rt) Then
                _poco.Add(rt, selSchema)
            End If
        End Sub

        Public Function ToPOCOList(Of T As {New, Class})(ByVal mgr As OrmManager) As IList(Of T)
            If mgr Is Nothing Then
                Throw New ArgumentNullException("mgr")
            End If

            Dim rt As Type = GetType(T)
            Dim mpe As ObjectMappingEngine = mgr.MappingEngine

            Dim hasPK As Boolean = False
            Dim selSchema As IEntitySchema = mpe.GetEntitySchema(rt, False)

            If selSchema Is Nothing AndAlso _poco IsNot Nothing Then
                selSchema = CType(_poco(rt), IEntitySchema)
            End If

            If selSchema Is Nothing Then
                selSchema = GetPOCOSchema(mpe, rt, hasPK)
                'If Not mpe.HasEntitySchema(rt) Then
                AddPOCO(rt, selSchema)
                'End If
            Else
                If FromClause Is Nothing Then
                    Me.From(selSchema.Table)
                End If
                hasPK = selSchema.GetPK IsNot Nothing
                _pocoType = rt
            End If

            Dim l As IEnumerable = Nothing
            Dim r As New List(Of T)

            'Using New ObjectMappingEngine.CustomTypes(mpe, GetCustomTypes)
            If hasPK Then
                l = ToObjectList(Of AnonymousCachedEntity)(mgr)
            Else
                l = ToObjectList(Of AnonymousEntity)(mgr)
            End If
            'End Using

            'Dim props As IDictionary = mpe.GetProperties(rt, selSchema)
            For Each e As _IEntity In l
                Dim ctd As ComponentModel.ICustomTypeDescriptor = CType(e, ComponentModel.ICustomTypeDescriptor)
                Dim ro As New T
                InitPOCO(rt, ctd, mpe, e, ro, mgr.Cache, mgr.ContextInfo)
                r.Add(ro)
            Next

            Return r
        End Function

        Private Sub InitPOCO(ByVal rt As Type,
                             ByVal ctd As ComponentModel.ICustomTypeDescriptor, ByVal mpe As ObjectMappingEngine,
                             ByVal e As _IEntity, ByVal ro As Object, ByVal cache As Cache.CacheBase, ByVal contextInfo As Object,
                             Optional ByVal pref As String = Nothing)

            Dim oschema As IEntitySchema = Nothing
            If _poco IsNot Nothing Then
                oschema = CType(_poco(rt), IEntitySchema)
            End If
            If oschema Is Nothing Then
                oschema = mpe.GetEntitySchema(rt)
            End If

            Dim map As Collections.IndexedCollection(Of String, MapField2Column) = oschema.FieldColumnMap
            For Each m As MapField2Column In map
                Dim pa As String = m.PropertyAlias
                If ctd.GetProperties.Find(pa, False) Is Nothing Then
                    pa = rt.Name & "-" & pa
                    If Not String.IsNullOrEmpty(pref) Then
                        pa = "%" & pref & "-" & pa
                    End If
                    If ctd.GetProperties.Find(pa, False) Is Nothing Then
                        Continue For
                    End If
                End If

                Dim pi As Reflection.PropertyInfo = m.PropertyInfo
                If pi Is Nothing Then
                    Dim ll As List(Of EntityPropertyAttribute) = ObjectMappingEngine.GetMappedProperties(rt, mpe.Version, True, True, mpe.ConvertVersionToInt, mpe.Features)
                    For Each item As EntityPropertyAttribute In ll
                        If item.PropertyAlias = m.PropertyAlias Then
                            pi = item._pi
                            m.PropertyInfo = pi
                            Exit For
                        End If
                    Next
                End If
                Dim v As Object = ObjectMappingEngine.GetPropertyValue(e, pa, oschema, pi)
                If v Is DBNull.Value Then
                    v = Nothing
                End If
                'pi.SetValue(ro, v, Nothing)

                Dim vals As IPKDesc = Nothing
                'If ObjectMappingEngine.IsEntityType(pit, mpe) Then
                '    vals = mpe.GetPKs(v, GetEntitySchema(mpe, pit))
                'Else
                vals = New PKDesc(New ColumnValue(pa, v))
                'End If

                'v = ObjectMappingEngine.AssignValue2Property(pit, mpe, cache, vals, ro, map, m.PropertyAlias, TryCast(ro, IPropertyLazyLoad), m, oschema, Nothing, CreateManager)
                Dim newo = mpe.ParseValueFromStorage(m.Attributes, ro, m, m.PropertyAlias, oschema, cache, CreateManager, vals, TryCast(ro, IPropertyLazyLoad), Nothing)
                Dim pit As Type = newo?.GetType
                If newo IsNot Nothing AndAlso _poco IsNot Nothing AndAlso _poco.Contains(pit) Then
                    InitPOCO(pit, ctd, mpe, e, v, cache, contextInfo, m.PropertyAlias)
                End If
            Next
        End Sub
#End Region

#Region " Singles "

        Public Function [Single](ByVal mgr As OrmManager) As Object
            Dim l As IList = ToList(mgr)
            If l.Count <> 1 Then
                Throw New InvalidOperationException("Number of items is " & l.Count)
            End If
            Return l(0)
        End Function

        Public Function [Single](ByVal getMgr As ICreateManager) As Object
            Dim l As IList = ToList(getMgr)
            If l.Count <> 1 Then
                Throw New InvalidOperationException("Number of items is " & l.Count)
            End If
            Return l(0)
        End Function

        Public Function [Single]() As Object
            Dim l As IList = ToList()
            If l.Count <> 1 Then
                Throw New InvalidOperationException("Number of items is " & l.Count)
            End If
            Return l(0)
        End Function

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
#End Region

#Region " SingleOrDefault "
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
#End Region

#Region " SingleDyn "
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

#End Region

#Region " SingleOrDefaultDyn "

        Public Function [SingleOrDefaultDyn](Of T As _ICachedEntity)(ByVal mgr As OrmManager) As T
            Dim l As ReadOnlyEntityList(Of T) = ToEntityList(Of T)(mgr)
            If l.Count > 1 Then
                Throw New InvalidOperationException("Number of items is " & l.Count)
            ElseIf l.Count = 1 Then
                Return l(0)
            End If
            Return Nothing
        End Function

        Public Function SingleOrDefaultDyn(Of T As _ICachedEntity)(ByVal getMgr As ICreateManager) As T
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
            If l.Count > 1 Then
                Throw New InvalidOperationException("Number of items is " & l.Count)
            ElseIf l.Count = 1 Then
                Return l(0)
            End If
            Return Nothing
        End Function

        Public Function [SingleOrDefaultSimple](Of T)(ByVal getMgr As ICreateManager) As T
            Dim l As IList(Of T) = ToSimpleList(Of T)(getMgr)
            If l.Count > 1 Then
                Throw New InvalidOperationException("Number of items is " & l.Count)
            ElseIf l.Count = 1 Then
                Return l(0)
            End If
            Return Nothing
        End Function

        Public Function [SingleOrDefaultSimple](Of T)() As T
            Dim l As IList(Of T) = ToSimpleList(Of T)()
            If l.Count > 1 Then
                Throw New InvalidOperationException("Number of items is " & l.Count)
            ElseIf l.Count = 1 Then
                Return l(0)
            End If
            Return Nothing
        End Function
#End Region

#End Region

#Region " Firsts "

#Region " First "
        Public Function First(Of T As {New, _ICachedEntity})(ByVal mgr As OrmManager) As T
            Dim oldT As Top = TopParam
            Dim oldRowFilter As TableFilter = RowNumberFilter
            Try

                Dim l As ReadOnlyEntityList(Of T) = Nothing
                If RowNumberFilter Is Nothing Then
                    l = Top(1).ToList(Of T)(mgr)
                Else
                    l = Take(1).ToList(Of T)(mgr)
                End If

                If l.Count = 0 Then
                    Throw New InvalidOperationException("Number of items is " & l.Count)
                End If
                Return l(0)
            Finally
                _top = oldT
                _rn = oldRowFilter
            End Try
        End Function

        Public Function First(Of T As {New, _ICachedEntity})(ByVal getMgr As ICreateManager) As T
            Dim oldT As Top = TopParam
            Dim oldRowFilter As TableFilter = RowNumberFilter
            Try
                Dim l As ReadOnlyEntityList(Of T) = Nothing
                If RowNumberFilter Is Nothing Then
                    l = Top(1).ToList(Of T)(getMgr)
                Else
                    l = Take(1).ToList(Of T)(getMgr)
                End If
                If l.Count = 0 Then
                    Throw New InvalidOperationException("Number of items is " & l.Count)
                End If
                Return l(0)
            Finally
                _top = oldT
                _rn = oldRowFilter
            End Try
        End Function
        Public Function First(Of T As {New, _ICachedEntity})(ByVal getMgr As CreateManagerDelegate) As T
            Dim oldT As Top = TopParam
            Dim oldRowFilter As TableFilter = RowNumberFilter
            Try
                Dim l As ReadOnlyEntityList(Of T) = Nothing
                If RowNumberFilter Is Nothing Then
                    l = Top(1).ToList(Of T)(getMgr)
                Else
                    l = Take(1).ToList(Of T)(getMgr)
                End If
                If l.Count = 0 Then
                    Throw New InvalidOperationException("Number of items is " & l.Count)
                End If
                Return l(0)
            Finally
                _top = oldT
                _rn = oldRowFilter
            End Try
        End Function

        Public Function First(Of T As {New, _ICachedEntity})() As T
            Dim oldT As Top = TopParam
            Dim oldRowFilter As TableFilter = RowNumberFilter
            Try
                Dim l As ReadOnlyEntityList(Of T) = Nothing
                If RowNumberFilter Is Nothing Then
                    l = Top(1).ToList(Of T)()
                Else
                    l = Take(1).ToList(Of T)()
                End If
                If l.Count = 0 Then
                    Throw New InvalidOperationException("Number of items is " & l.Count)
                End If
                Return l(0)
            Finally
                _top = oldT
            End Try
        End Function
#End Region

#Region " FirstOrDefault "
        Public Function FirstOrDefault(Of T As {New, _ICachedEntity})(ByVal mgr As OrmManager) As T
            Dim oldT As Top = TopParam
            Try
                Dim l As ReadOnlyEntityList(Of T) = Top(1).ToList(Of T)(mgr)
                If l.Count = 0 Then
                    Return Nothing
                End If
                Return l(0)
            Finally
                _top = oldT
            End Try
        End Function

        Public Function FirstOrDefault(Of T As {New, _ICachedEntity})(ByVal getMgr As ICreateManager) As T
            Dim oldT As Top = TopParam
            Try
                Dim l As ReadOnlyEntityList(Of T) = Top(1).ToList(Of T)(getMgr)
                If l.Count = 0 Then
                    Return Nothing
                End If
                Return l(0)
            Finally
                _top = oldT
            End Try
        End Function

        Public Function FirstOrDefault(Of T As {New, _ICachedEntity})() As T
            Dim oldT As Top = TopParam
            Try
                Dim l As ReadOnlyEntityList(Of T) = Top(1).ToList(Of T)()
                If l.Count = 0 Then
                    Return Nothing
                End If
                Return l(0)
            Finally
                _top = oldT
            End Try
        End Function
#End Region

#Region " FirstSimple "

        Public Function FirstSimple(Of T)(ByVal mgr As OrmManager) As T
            Dim oldT As Top = TopParam
            Dim oldRowFilter As TableFilter = RowNumberFilter
            Try

                Dim l As IList(Of T) = Nothing
                If RowNumberFilter Is Nothing Then
                    l = Top(1).ToSimpleList(Of T)(mgr)
                Else
                    l = Take(1).ToSimpleList(Of T)(mgr)
                End If

                If l.Count = 0 Then
                    Throw New InvalidOperationException("Number of items is " & l.Count)
                End If
                Return l(0)
            Finally
                _top = oldT
                _rn = oldRowFilter
            End Try
        End Function

        Public Function FirstSimple(Of T)(ByVal getMgr As ICreateManager) As T
            Dim oldT As Top = TopParam
            Dim oldRowFilter As TableFilter = RowNumberFilter
            Try

                Dim l As IList(Of T) = Nothing
                If RowNumberFilter Is Nothing Then
                    l = Top(1).ToSimpleList(Of T)(getMgr)
                Else
                    l = Take(1).ToSimpleList(Of T)(getMgr)
                End If

                If l.Count = 0 Then
                    Throw New InvalidOperationException("Number of items is " & l.Count)
                End If
                Return l(0)
            Finally
                _top = oldT
                _rn = oldRowFilter
            End Try
        End Function

        Public Function FirstSimple(Of T)() As T
            Dim oldT As Top = TopParam
            Dim oldRowFilter As TableFilter = RowNumberFilter
            Try

                Dim l As IList(Of T) = Nothing
                If RowNumberFilter Is Nothing Then
                    l = Top(1).ToSimpleList(Of T)()
                Else
                    l = Take(1).ToSimpleList(Of T)()
                End If

                If l.Count = 0 Then
                    Throw New InvalidOperationException("Number of items is " & l.Count)
                End If
                Return l(0)
            Finally
                _top = oldT
                _rn = oldRowFilter
            End Try
        End Function
#End Region

#Region " FirstOrDefaultSimple "
        Public Function [FirstOrDefaultSimple](Of T)(ByVal mgr As OrmManager) As T
            Dim oldT As Top = TopParam
            Try
                Dim l As IList(Of T) = Top(1).ToSimpleList(Of T)(mgr)
                If l.Count = 0 Then
                    Return Nothing
                End If
                Return l(0)
            Finally
                _top = oldT
            End Try
        End Function

        Public Function [FirstOrDefaultSimple](Of T)(ByVal getMgr As ICreateManager) As T
            Dim oldT As Top = TopParam
            Try
                Dim l As IList(Of T) = Top(1).ToSimpleList(Of T)(getMgr)
                If l.Count = 0 Then
                    Return Nothing
                End If
                Return l(0)
            Finally
                _top = oldT
            End Try
        End Function

        Public Function [FirstOrDefaultSimple](Of T)() As T
            Dim oldT As Top = TopParam
            Try
                Dim l As IList(Of T) = Top(1).ToSimpleList(Of T)()
                If l.Count = 0 Then
                    Return Nothing
                End If
                Return l(0)
            Finally
                _top = oldT
            End Try
        End Function
#End Region

#Region " FirstEntity "

        Public Function FirstEntity(Of T As _IEntity)(ByVal mgr As OrmManager) As T
            Dim oldT As Top = TopParam
            Dim oldRowFilter As TableFilter = RowNumberFilter
            Try

                Dim l As IList(Of T) = Nothing
                If RowNumberFilter Is Nothing Then
                    l = Top(1).ToObjectList(Of T)(mgr)
                Else
                    l = Take(1).ToObjectList(Of T)(mgr)
                End If

                If l.Count = 0 Then
                    Throw New InvalidOperationException("Number of items is " & l.Count)
                End If
                Return l(0)
            Finally
                _top = oldT
                _rn = oldRowFilter
            End Try
        End Function

        Public Function FirstEntity(Of T As _IEntity)(ByVal getMgr As ICreateManager) As T
            Dim oldT As Top = TopParam
            Dim oldRowFilter As TableFilter = RowNumberFilter
            Try

                Dim l As IList(Of T) = Nothing
                If RowNumberFilter Is Nothing Then
                    l = Top(1).ToObjectList(Of T)(getMgr)
                Else
                    l = Take(1).ToObjectList(Of T)(getMgr)
                End If

                If l.Count = 0 Then
                    Throw New InvalidOperationException("Number of items is " & l.Count)
                End If
                Return l(0)
            Finally
                _top = oldT
                _rn = oldRowFilter
            End Try
        End Function
        Public Function FirstEntity(Of T As _IEntity)(getMgr As CreateManagerDelegate) As T
            Dim oldT As Top = TopParam
            Dim oldRowFilter As TableFilter = RowNumberFilter
            Try

                Dim l As IList(Of T) = Nothing
                If RowNumberFilter Is Nothing Then
                    l = Top(1).ToObjectList(Of T)(getMgr)
                Else
                    l = Take(1).ToObjectList(Of T)(getMgr)
                End If

                If l.Count = 0 Then
                    Throw New InvalidOperationException("Number of items is " & l.Count)
                End If
                Return l(0)
            Finally
                _top = oldT
                _rn = oldRowFilter
            End Try
        End Function
        Public Function FirstEntity(Of T As _IEntity)() As T
            Dim oldT As Top = TopParam
            Dim oldRowFilter As TableFilter = RowNumberFilter
            Try

                Dim l As IList(Of T) = Nothing
                If RowNumberFilter Is Nothing Then
                    l = Top(1).ToObjectList(Of T)()
                Else
                    l = Take(1).ToObjectList(Of T)()
                End If

                If l.Count = 0 Then
                    Throw New InvalidOperationException("Number of items is " & l.Count)
                End If
                Return l(0)
            Finally
                _top = oldT
                _rn = oldRowFilter
            End Try
        End Function
#End Region

#Region " FirstOrDefaultEntity "
        Public Function [FirstOrDefaultEntity](Of T As _IEntity)(ByVal mgr As OrmManager) As T
            Dim oldT As Top = TopParam
            Try
                Dim l As IList(Of T) = Top(1).ToObjectList(Of T)(mgr)
                If l.Count = 0 Then
                    Return Nothing
                End If
                Return l(0)
            Finally
                _top = oldT
            End Try
        End Function

        Public Function [FirstOrDefaultEntity](Of T As _IEntity)(ByVal getMgr As ICreateManager) As T
            Dim oldT As Top = TopParam
            Try
                Dim l As IList(Of T) = Top(1).ToObjectList(Of T)(getMgr)
                If l.Count = 0 Then
                    Return Nothing
                End If
                Return l(0)
            Finally
                _top = oldT
            End Try
        End Function

        Public Function [FirstOrDefaultEntity](Of T As _IEntity)() As T
            Dim oldT As Top = TopParam
            Try
                Dim l As IList(Of T) = Top(1).ToObjectList(Of T)()
                If l.Count = 0 Then
                    Return Nothing
                End If
                Return l(0)
            Finally
                _top = oldT
            End Try
        End Function
#End Region

#End Region

#Region " Lasts "

#Region " Last "
        Public Function Last(Of T As {New, _ICachedEntity})(ByVal mgr As OrmManager) As T
            Dim l As ReadOnlyEntityList(Of T) = Nothing
            If Sort Is Nothing Then
                l = ToList(Of T)(mgr)
            Else
                Dim sort As OrderByClause = Me.Sort
                Dim newSort As New List(Of SortExpression)
                For Each ns As SortExpression In sort
                    Dim cln As SortExpression = CType(ns.Clone, SortExpression)
                    newSort.Add(cln)
                    Dim newOrder As SortExpression.SortType = SortExpression.SortType.Asc
                    If cln.Order = SortExpression.SortType.Asc Then
                        newOrder = SortExpression.SortType.Desc
                    End If
                    cln.Order = newOrder
                Next
                Dim oldT As Top = TopParam
                Try
                    l = OrderBy(New OrderByClause(newSort)).Top(1).ToList(Of T)(mgr)
                Finally
                    _top = oldT
                    _order = sort
                End Try
            End If
            If l.Count = 0 Then
                Throw New InvalidOperationException("Number of items is " & l.Count)
            End If
            Return l(l.Count - 1)
        End Function

        Public Function Last(Of T As {New, _ICachedEntity})(ByVal getMgr As ICreateManager) As T
            Dim l As ReadOnlyEntityList(Of T) = Nothing
            If Sort Is Nothing Then
                l = ToList(Of T)(getMgr)
            Else
                Dim sort As OrderByClause = Me.Sort
                Dim newSort As New List(Of SortExpression)
                For Each ns As SortExpression In sort
                    Dim cln As SortExpression = CType(ns.Clone, SortExpression)
                    newSort.Add(cln)
                    Dim newOrder As SortExpression.SortType = SortExpression.SortType.Asc
                    If cln.Order = SortExpression.SortType.Asc Then
                        newOrder = SortExpression.SortType.Desc
                    End If
                    cln.Order = newOrder
                Next
                Dim oldT As Top = TopParam
                Try
                    l = OrderBy(New OrderByClause(newSort)).Top(1).ToList(Of T)(getMgr)
                Finally
                    _top = oldT
                    _order = sort
                End Try
            End If
            If l.Count = 0 Then
                Throw New InvalidOperationException("Number of items is " & l.Count)
            End If
            Return l(l.Count - 1)
        End Function

        Public Function Last(Of T As {New, _ICachedEntity})() As T
            Dim l As ReadOnlyEntityList(Of T) = Nothing
            If Sort Is Nothing Then
                l = ToList(Of T)()
            Else
                Dim sort As OrderByClause = Me.Sort
                Dim newSort As New List(Of SortExpression)
                For Each ns As SortExpression In sort
                    Dim cln As SortExpression = CType(ns.Clone, SortExpression)
                    newSort.Add(cln)
                    Dim newOrder As SortExpression.SortType = SortExpression.SortType.Asc
                    If cln.Order = SortExpression.SortType.Asc Then
                        newOrder = SortExpression.SortType.Desc
                    End If
                    cln.Order = newOrder
                Next
                Dim oldT As Top = TopParam
                Try
                    l = OrderBy(New OrderByClause(newSort)).Top(1).ToList(Of T)()
                Finally
                    _top = oldT
                    _order = sort
                End Try
            End If
            If l.Count = 0 Then
                Throw New InvalidOperationException("Number of items is " & l.Count)
            End If
            Return l(l.Count - 1)
        End Function
#End Region

#Region " LastOrDefault "
        Public Function LastOrDefault(Of T As {New, _ICachedEntity})(ByVal mgr As OrmManager) As T
            Dim l As ReadOnlyEntityList(Of T) = Nothing
            If Sort Is Nothing Then
                l = ToList(Of T)(mgr)
            Else
                Dim sort As OrderByClause = Me.Sort
                Dim newSort As New List(Of SortExpression)
                For Each ns As SortExpression In sort
                    Dim cln As SortExpression = CType(ns.Clone, SortExpression)
                    newSort.Add(cln)
                    Dim newOrder As SortExpression.SortType = SortExpression.SortType.Asc
                    If cln.Order = SortExpression.SortType.Asc Then
                        newOrder = SortExpression.SortType.Desc
                    End If
                    cln.Order = newOrder
                Next
                Dim oldT As Top = TopParam
                Try
                    l = OrderBy(New OrderByClause(newSort)).Top(1).ToList(Of T)(mgr)
                Finally
                    _top = oldT
                    _order = sort
                End Try
            End If
            If l.Count = 0 Then
                Return Nothing
            End If
            Return l(l.Count - 1)
        End Function

        Public Function LastOrDefault(Of T As {New, _ICachedEntity})(ByVal getMgr As ICreateManager) As T
            Dim l As ReadOnlyEntityList(Of T) = Nothing
            If Sort Is Nothing Then
                l = ToList(Of T)(getMgr)
            Else
                Dim sort As OrderByClause = Me.Sort
                Dim newSort As New List(Of SortExpression)
                For Each ns As SortExpression In sort
                    Dim cln As SortExpression = CType(ns.Clone, SortExpression)
                    newSort.Add(cln)
                    Dim newOrder As SortExpression.SortType = SortExpression.SortType.Asc
                    If cln.Order = SortExpression.SortType.Asc Then
                        newOrder = SortExpression.SortType.Desc
                    End If
                    cln.Order = newOrder
                Next
                Dim oldT As Top = TopParam
                Try
                    l = OrderBy(New OrderByClause(newSort)).Top(1).ToList(Of T)(getMgr)
                Finally
                    _top = oldT
                    _order = sort
                End Try
            End If
            If l.Count = 0 Then
                Return Nothing
            End If
            Return l(l.Count - 1)
        End Function

        Public Function LastOrDefault(Of T As {New, _ICachedEntity})() As T
            Dim l As ReadOnlyEntityList(Of T) = Nothing
            If Sort Is Nothing Then
                l = ToList(Of T)()
            Else
                Dim sort As OrderByClause = Me.Sort
                Dim newSort As New List(Of SortExpression)
                For Each ns As SortExpression In sort
                    Dim cln As SortExpression = CType(ns.Clone, SortExpression)
                    newSort.Add(cln)
                    Dim newOrder As SortExpression.SortType = SortExpression.SortType.Asc
                    If cln.Order = SortExpression.SortType.Asc Then
                        newOrder = SortExpression.SortType.Desc
                    End If
                    cln.Order = newOrder
                Next
                Dim oldT As Top = TopParam
                Try
                    l = OrderBy(New OrderByClause(newSort)).Top(1).ToList(Of T)()
                Finally
                    _top = oldT
                    _order = sort
                End Try
            End If
            If l.Count = 0 Then
                Return Nothing
            End If
            Return l(l.Count - 1)
        End Function
#End Region

#End Region

        Friend Function GetFieldsIdx(ByVal mpe As ObjectMappingEngine, ByVal t As Type) As Collections.IndexedCollection(Of String, MapField2Column)
            'Dim c As New OrmObjectIndex
            Dim c = GetFieldsIdx(mpe)
            Dim ll As List(Of EntityPropertyAttribute) = ObjectMappingEngine.GetMappedProperties(t, mpe.Version, True, True, mpe.ConvertVersionToInt, mpe.Features)
            ObjectMappingEngine.ApplyAttributes2Schema(c, ll, mpe, FromClause.Table)
            Return c
        End Function

        Friend Function GetFieldsIdx(mpe As ObjectMappingEngine) As Collections.IndexedCollection(Of String, MapField2Column)
            Dim c As New OrmObjectIndex

            'For Each p As SelectExpression In q.SelectList
            '    c.Add(New MapField2Column(p.Field, p.Column, p.Table, p.Attributes))
            'Next

            If _sl IsNot Nothing Then
                SelectExpression.GetMapping(c, _sl, mpe, Me)
            Else
                SelectExpression.GetMapping(c, SelectList, mpe, Me)
            End If
            'If q.Aggregates IsNot Nothing Then
            '    For Each p As AggregateBase In q.Aggregates
            '        c.Add(New MapField2Column(p.Alias, p.Alias, Nothing))
            '    Next
            'End If

            Return c
        End Function

        Private Function GetPOCOSchema(ByVal mpe As ObjectMappingEngine, ByVal t As Type,
                                       ByRef hasPK As Boolean) As IEntitySchema
            hasPK = False
            Dim s As IEntitySchema = mpe.GetPOCOEntitySchema(t)
            If s Is Nothing Then
                Dim fields As Collections.IndexedCollection(Of String, MapField2Column) = GetFieldsIdx(mpe, t)
                'If fields.Count = 0 Then
                '    fields = GetFieldsIdx(mpe)
                'End If

                If fields.Count = 0 Then
                    Throw New QueryCmdException(String.Format("Unable to map {0}", t), Me)
                End If

                s = New SimpleObjectSchema(fields)
                '                Dim tbl As SourceFragment = _from.Table
                '                If tbl Is Nothing AndAlso SelectList IsNot Nothing Then
                '                    For Each se As SelectExpression In SelectList
                '                        For Each e As IExpression In se.GetExpressions
                '                            Dim te As TableExpression = TryCast(e, TableExpression)
                '                            If te IsNot Nothing Then
                '                                tbl = te.SourceFragment
                '                                GoTo exit_for
                '                            Else
                '                                Dim ee As EntityExpression = TryCast(e, EntityExpression)
                '                                If ee IsNot Nothing Then
                '                                    Dim rt As Type = ee.ObjectProperty.Entity.GetRealType(mpe)
                '                                    If rt IsNot t Then
                '                                        Dim esch As IEntitySchema = mpe.GetEntitySchema(rt)
                '                                        If esch IsNot Nothing Then
                '                                            tbl = esch.Table
                '                                            GoTo exit_for
                '                                        End If
                '                                    End If
                '                                End If
                '                            End If
                '                        Next
                '                    Next
                'exit_for:
                '                End If
                '                If tbl Is Nothing Then
                '                    Throw New QueryCmdException(String.Format("Cannot create schema for type {0}. QueryCmd has empty FromClause or else entity has no EntityAttribute", t), Me)
                '                End If
                '                s = mpe.CreateAndInitSchemaAndNames(t, New EntityAttribute(mpe.Version) With {._tbl = tbl, .RawProperties = True})
            End If
            hasPK = s.GetPK IsNot Nothing
            Return s
        End Function

        Protected Friend Function GetSchemaForSelectType(ByVal mpe As ObjectMappingEngine) As IEntitySchema
            Dim t As Type = GetSelectedType(mpe)
            'If CreateType Is Nothing OrElse t Is Nothing Then
            '    Throw New QueryCmdException("Neither Into clause not specified nor ToAnonymous used", Me)
            'End If

            Dim ct As EntityUnion = CreateType
            If t Is Nothing Then
                If ct IsNot Nothing Then
                    Return mpe.GetEntitySchema(ct.GetRealType(mpe), False)
                End If
            Else
                If ct Is Nothing OrElse ct.GetRealType(mpe).IsAssignableFrom(t) Then
                    Return mpe.GetEntitySchema(t, False)
                ElseIf ct IsNot Nothing AndAlso t.IsAssignableFrom(ct.GetRealType(mpe)) Then
                    Return mpe.GetEntitySchema(ct.GetRealType(mpe), False)
                End If
            End If

            If _poco Is Nothing Then
                Return Nothing
            Else
                For Each de As DictionaryEntry In _poco
                    Return CType(de.Value, IEntitySchema)
                Next
                Throw New QueryCmdException("impossible", Me)
            End If
        End Function

        Public Sub ClearCache(ByVal mgr As OrmManager)
            If mgr Is Nothing Then
                Throw New ArgumentNullException("mgr")
            End If

            GetExecutor(mgr).ClearCache(mgr, Me)
        End Sub

        Public Sub RenewCache(ByVal mgr As OrmManager, ByVal v As Boolean)
            If mgr Is Nothing Then
                Throw New ArgumentNullException("mgr")
            End If

            GetExecutor(mgr).RenewCache(mgr, Me, v)
        End Sub

        Public Function IsInCache(ByVal mgr As OrmManager) As Boolean
            If mgr Is Nothing Then
                Throw New ArgumentNullException("mgr")
            End If

            Return GetExecutor(mgr).IsInCache(mgr, Me)
        End Function

        Public Sub ClearCache()
            Using gm = GetManager(_getMgr)
                If gm Is Nothing Then
                    Throw New QueryCmdException("OrmManager required", Me)
                End If

                GetExecutor(gm.Manager).ClearCache(gm.Manager, Me)
            End Using
        End Sub

        Public Sub RenewCache(ByVal v As Boolean)
            Using gm = GetManager(_getMgr)
                If gm Is Nothing Then
                    Throw New QueryCmdException("OrmManager required", Me)
                End If

                GetExecutor(gm.Manager).RenewCache(gm.Manager, Me, v)
            End Using
        End Sub

        Public Function IsInCache() As Boolean

            Using gm = GetManager(_getMgr)
                If gm Is Nothing Then
                    Throw New QueryCmdException("OrmManager required", Me)
                End If

                Return GetExecutor(gm.Manager).IsInCache(gm.Manager, Me)
            End Using

        End Function

        Public Sub ResetObjects()
            ResetObjects(_getMgr)
        End Sub

        Public Sub ResetObjects(ByVal getMgr As ICreateManager)
            Using gm = GetManager(getMgr)
                If gm Is Nothing Then
                    Throw New QueryCmdException("OrmManager required", Me)
                End If

                ResetObjects(gm.Manager)
            End Using
        End Sub

        Public Sub ResetObjects(ByVal mgr As OrmManager)
            If mgr Is Nothing Then
                Throw New ArgumentNullException("mgr")
            End If

            GetExecutor(mgr).ResetObjects(mgr, Me)
        End Sub

        Public Overridable Function CopyTo(ByVal o As QueryCmd) As Boolean
            If o Is Nothing Then
                Return False
            End If

            With o
                '._aggregates = _aggregates
                ._appendMain = _appendMain
                ._autoJoins = _autoJoins
                ._clientPage = _clientPage
                ._distinct = _distinct
                ._dontcache = _dontcache
                ._hint = _hint
                '._m2mKey = _m2mKey
                '._load = _load
                ._mark = ._mark
                '._m2mObject = _m2mObject
                ._notSimpleMode = _notSimpleMode
                ._statementMark = _statementMark
                ._er = _er
                ._resDic = _resDic
                ._appendMain = _appendMain
                ._getMgr = _getMgr
                ._liveTime = _liveTime
                ._mgrMark = _mgrMark
                ._name = _name
                ._execCnt = _execCnt
                ._schema = _schema
                ._cacheSort = _cacheSort
                ._timeout = _timeout
                ._autoFields = _autoFields
                ._context = _context
            End With

            If _exec IsNot Nothing Then
                o._exec = CType(_exec.Clone, IExecutor)
            End If

            If _sel IsNot Nothing Then
                o._sel = _sel.Clone
            End If

            If _filter IsNot Nothing Then
                o._filter = _filter.Filter.Clone
            End If

            If _group IsNot Nothing Then
                o._group = _group.Clone
            End If

            If _joins IsNot Nothing Then
                Dim l As New List(Of QueryJoin)
                For Each j In _joins
                    l.Add(j.Clone)
                Next
                o._joins = l.ToArray
            End If

            If _order IsNot Nothing Then
                Dim l As New List(Of SortExpression)
                For Each j In _order
                    l.Add(j.Clone)
                Next
                o._order = New OrderByClause(l)
            End If

            If _includeFields IsNot Nothing Then
                o._includeFields = New Dictionary(Of String, Pair(Of Type, List(Of String)))(_includeFields)
            End If

            If _from IsNot Nothing Then
                o._from = _from.Clone
            End If

            o._top = _top
            If _rn IsNot Nothing Then
                o._rn = _rn.Clone
            End If

            If _having IsNot Nothing Then
                o._having = _having.Filter.Clone
            End If

            If _unions IsNot Nothing Then
                Dim l As New List(Of Pair(Of Boolean, QueryCmd))
                For Each j In _unions
                    Dim q As QueryCmd = Nothing
                    If j.Second IsNot Nothing Then
                        q = j.Second.Clone
                    End If

                    l.Add(New Pair(Of Boolean, QueryCmd)(j.First, q))
                Next
                o._unions = New ReadOnlyCollection(Of Pair(Of Boolean, QueryCmd))(l)
            End If

            If _includeEntities IsNot Nothing Then
                Dim l As New List(Of EntityUnion)
                For Each j In _includeEntities
                    l.Add(j.Clone)
                Next
                o._includeEntities = l
            End If

            If _poco IsNot Nothing Then
                o._poco = Hashtable.Synchronized(New Hashtable(_poco))
            End If

            If _optimizeIn IsNot Nothing Then
                o._optimizeIn = _optimizeIn.Clone
            End If

            If _optimizeOr IsNot Nothing Then
                o._optimizeOr = New List(Of Criteria.PredicateLink)(_optimizeOr)
            End If

            If _newMaps IsNot Nothing Then
                o._newMaps = Hashtable.Synchronized(New Hashtable(_newMaps))
            End If
            Return True
        End Function

        Protected Overridable Function _Clone() As Object Implements System.ICloneable.Clone
            Return Clone()
        End Function

        Public Function Clone() As QueryCmd
            Dim q As New QueryCmd
            CopyTo(q)
            Return q
        End Function

        Protected Overridable Function _CopyTo(target As ICopyable) As Boolean Implements ICopyable.CopyTo
            Return CopyTo(TryCast(target, QueryCmd))
        End Function

        Private Function _FindColumn(ByVal mpe As ObjectMappingEngine, ByVal p As String) As String()
            For Each se As SelectExpression In _sl
                'If se.PropType = PropType.ObjectProperty Then
                If se.GetIntoPropertyAlias(True) = p Then
                    If Not String.IsNullOrEmpty(se.ColumnAlias) Then
                        Return New String() {se.ColumnAlias}
                    Else
                        Dim te As TableExpression = TryCast(se.Operand, TableExpression)
                        If te IsNot Nothing Then
                            Return New String() {te.SourceField}
                        Else
                            Dim col = GetSelectedEntities(se)
                            'If col.Count <> 1 Then
                            '    Throw New QueryCmdException("", Me)
                            'End If
                            For Each su As SelectUnion In col
                                If su.EntityUnion IsNot Nothing Then
                                    Dim map As MapField2Column = GetFieldColumnMap(_types(su.EntityUnion), su.EntityUnion.GetRealType(mpe))(p)
                                    Return map.SourceFields.Select(Function(item) CoreFramework.StringExtensions.Coalesce(item.SourceFieldAlias, item.SourceFieldExpression)).ToArray
                                End If
                            Next

                            'If _from.AnyQuery IsNot Nothing Then
                            '    Return _from.AnyQuery.FindColumn(mpe, p)
                            'End If
                        End If
                    End If
                    'Else
                    '    If se.PropertyAlias = p Then
                    '        Dim t As Type = se.ObjectSource.GetRealType(mpe)
                    '        If t Is Nothing Then
                    '            Return _from.AnyQuery.FindColumn(mpe, p)
                    '        Else
                    '            Dim oschema As IEntitySchema = GetEntitySchema(mpe, t)
                    '            Dim map As MapField2Column = GetFieldColumnMap(oschema, t)(se.PropertyAlias)
                    '            Return map.ColumnExpression 'mpe.GetColumnNameByPropertyAlias(oschema, se.PropertyAlias, False, se.ObjectSource)
                    '        End If
                    '    End If
                End If
                'Else
                '    If se.Column = p Then
                '        Return p
                '    End If
                'End If
            Next
            Return Nothing
        End Function

        Private Function FindColumn(ByVal mpe As ObjectMappingEngine, ByVal p As String) As String() Implements IExecutionContext.FindColumn
            Dim c() As String = Nothing
            If _from.AnyQuery IsNot Nothing Then
                c = _from.AnyQuery._FindColumn(mpe, p)
            End If

            If c Is Nothing Then
                c = _FindColumn(mpe, p)

                If c Is Nothing Then
                    Throw New QueryCmdException("Couldn't find column for property " & p, Me)
                End If
            End If

            Return c
        End Function

        Public Function GetDependentEntities(ByVal mpe As ObjectMappingEngine, ByRef fl As List(Of String)) As Dictionary(Of EntityUnion, List(Of String))
            Dim d As New Dictionary(Of EntityUnion, List(Of String))
            Dim fe As EntityUnion = Nothing
            If FromClause IsNot Nothing AndAlso FromClause.QueryEU IsNot Nothing Then
                fe = FromClause.QueryEU
                fl = New List(Of String)
            End If

            If _js IsNot Nothing Then
                For Each j As QueryJoin In _js
                    If j.ObjectSource IsNot Nothing Then
                        Dim l As List(Of String) = Nothing
                        If Not d.TryGetValue(j.ObjectSource, l) Then
                            l = New List(Of String)
                            d.Add(j.ObjectSource, l)
                        End If

                        If j.Condition IsNot Nothing Then
                            For Each f As IFilter In j.Condition.GetAllFilters
                                Dim ef As EntityFilter = TryCast(f, EntityFilter)
                                If ef IsNot Nothing Then
                                    xxx(ef.Template.ObjectSource, fe, ef.Template.PropertyAlias, fl, d)
                                Else
                                    Dim jf As JoinFilter = TryCast(f, JoinFilter)
                                    If jf IsNot Nothing Then
                                        If jf.Left.Property.Entity IsNot Nothing Then
                                            xxx(jf.Left.Property.Entity, fe, jf.Left.Property.PropertyAlias, fl, d)
                                        End If
                                        If jf.Right.Property.Entity IsNot Nothing Then
                                            xxx(jf.Right.Property.Entity, fe, jf.Right.Property.PropertyAlias, fl, d)
                                        End If
                                    End If
                                End If
                            Next
                        End If
                    End If
                Next
            End If

            Return d
        End Function

        Private Shared Sub xxx(ByVal eu As EntityUnion, ByVal fe As EntityUnion, ByVal prop As String, _
                               ByVal fl As List(Of String), ByVal d As Dictionary(Of EntityUnion, List(Of String)))
            If Not eu.Equals(fe) Then
                Dim ll As List(Of String) = Nothing
                If Not d.TryGetValue(eu, ll) Then
                    ll = New List(Of String)
                    d.Add(eu, ll)
                End If
                ll.Add(prop)
            Else
                If Not fl.Contains(prop) Then
                    fl.Add(prop)
                End If
            End If
        End Sub
        'Private Function [Get](ByVal mpe As ObjectMappingEngine) As Cache.IDependentTypes Implements Cache.IQueryDependentTypes.Get
        '    'If SelectedType Is Nothing Then
        '    '    Return New Cache.EmptyDependentTypes
        '    'End If

        '    Dim dp As New Cache.DependentTypes
        '    Dim singleType As Boolean = True
        '    If _joins IsNot Nothing Then
        '        For Each j As QueryJoin In _joins
        '            If j.ObjectSource IsNot Nothing Then
        '                Dim t As Type = j.ObjectSource.GetRealType(mpe)
        '                'If t Is Nothing AndAlso Not String.IsNullOrEmpty(j.EntityName) Then
        '                '    t = mpe.GetTypeByEntityName(j.EntityName)
        '                'End If
        '                'If t Is Nothing Then
        '                '    Return New Cache.EmptyDependentTypes
        '                'End If
        '                If t IsNot Nothing Then
        '                    dp.AddBoth(t)
        '                    singleType = False
        '                End If
        '            End If
        '        Next
        '    End If

        '    Dim fromType As Type = Nothing
        '    If FromClause.QueryEU IsNot Nothing Then
        '        fromType = FromClause.QueryEU.GetRealType(mpe)
        '    End If

        '    If singleType AndAlso Not dp.IsEmpty Then
        '        dp.AddBoth(fromType)
        '    End If

        '    If _filter IsNot Nothing AndAlso TryCast(_filter, IEntityFilter) Is Nothing Then
        '        For Each f As IFilter In _filter.Filter.GetAllFilters
        '            Dim fdp As Cache.IDependentTypes = Cache.QueryDependentTypes(mpe, f)
        '            If Cache.IsCalculated(fdp) Then
        '                If singleType Then
        '                    dp.AddBoth(fromType)
        '                End If
        '                dp.Merge(fdp)
        '                'Else
        '                '    Return fdp
        '            End If
        '        Next
        '    End If

        '    If _order IsNot Nothing Then
        '        For Each ns As SortExpression In Sort
        '            Dim fdp As Cache.IDependentTypes = Cache.QueryDependentTypes(mpe, ns)
        '            If Cache.IsCalculated(fdp) Then
        '                If singleType Then
        '                    dp.AddBoth(fromType)
        '                End If
        '                dp.Merge(fdp)
        '            Else
        '                For Each s As SelectUnion In GetSelectedEntities(ns)
        '                    If s.EntityUnion IsNot Nothing Then
        '                        dp.AddUpdated(s.EntityUnion.GetRealType(mpe))
        '                    End If
        '                Next
        '            End If
        '        Next
        '    End If

        '    If _group IsNot Nothing Then
        '        For Each ge As IExpression In Group.GetExpressions
        '            For Each s As SelectUnion In GetSelectedEntities(ge)
        '                If s.EntityUnion IsNot Nothing Then
        '                    dp.AddDeleted(s.EntityUnion.GetRealType(mpe))
        '                End If
        '            Next
        '        Next
        '    End If

        '    If SelectList IsNot Nothing Then
        '        For Each se As SelectExpression In SelectList
        '            For Each s As SelectUnion In GetSelectedEntities(se)
        '                If s.EntityUnion IsNot Nothing Then
        '                    dp.AddBoth(s.EntityUnion.GetRealType(mpe))
        '                End If
        '            Next
        '        Next
        '    End If
        '    Return dp.Get
        'End Function
        Public Function Load(ByVal eu As EntityUnion) As QueryCmd
            Dim os As EntityUnion = GetSelectedOS()
            If os IsNot Nothing AndAlso os = eu Then
                Throw New NotSupportedException
            End If
            If _joins Is Nothing Then
                'Throw New QueryCmdException("Entity must be present among joins", Me)
                If Not _includeEntities.Contains(eu) Then
                    _includeEntities.Add(eu)
                End If
            Else
                For Each j As QueryJoin In _joins
                    If j.ObjectSource IsNot Nothing AndAlso j.ObjectSource = eu Then
                        If Not _includeEntities.Contains(j.ObjectSource) Then
                            _includeEntities.Add(j.ObjectSource)
                        End If
                        Return Me
                    End If
                Next
                'Throw New QueryCmdException("Entity must be present among joins", Me)
            End If

            Return Me
        End Function
        Public Function Load(ByVal entityName As String) As QueryCmd
            Dim os As EntityUnion = GetSelectedOS()
            If os IsNot Nothing AndAlso String.Equals(os.AnyEntityName, entityName) Then
                Throw New NotSupportedException
            End If
            If _joins Is Nothing Then
                'Throw New QueryCmdException("Entity must be present among joins", Me)
                If Not _includeEntities.Any(Function(j) j.AnyEntityName = entityName) Then
                    _includeEntities.Add(New EntityUnion(entityName))
                End If
            Else
                For Each j As QueryJoin In _joins
                    If j.ObjectSource IsNot Nothing AndAlso String.Equals(j.ObjectSource.AnyEntityName, entityName) Then
                        If Not _includeEntities.Contains(j.ObjectSource) Then
                            _includeEntities.Add(j.ObjectSource)
                        End If
                        Return Me
                    End If
                Next
                'Throw New QueryCmdException("Entity must be present among joins", Me)
            End If

            Return Me
        End Function

        Public Function Load(ByVal t As Type) As QueryCmd
            Dim os As EntityUnion = GetSelectedOS()
            If os IsNot Nothing AndAlso os.AnyType Is t Then
                Throw New NotSupportedException
            End If
            If _joins Is Nothing Then
                'Throw New QueryCmdException("Entity must be present among joins", Me)
                If Not _includeEntities.Any(Function(j) j.AnyType = t) Then
                    _includeEntities.Add(New EntityUnion(t))
                End If
            Else
                For Each j As QueryJoin In _joins
                    If j.ObjectSource IsNot Nothing AndAlso j.ObjectSource.AnyType Is t Then
                        If Not _includeEntities.Contains(j.ObjectSource) Then
                            _includeEntities.Add(j.ObjectSource)
                        End If
                        Return Me
                    End If
                Next
                'Throw New QueryCmdException("Entity must be present among joins", Me)
            End If

            Return Me
        End Function

        Public Function Load(ByVal al As QueryAlias) As QueryCmd
            Dim os As EntityUnion = GetSelectedOS()
            If os IsNot Nothing AndAlso os.ObjectAlias Is al Then
                Throw New NotSupportedException
            End If
            If _joins Is Nothing Then
                'Throw New QueryCmdException("Entity must be present among joins", Me)
                If Not _includeEntities.Any(Function(j) j.IsQuery AndAlso j.ObjectAlias Is al) Then
                    _includeEntities.Add(New EntityUnion(al))
                End If
            Else
                For Each j As QueryJoin In _joins
                    If j.ObjectSource IsNot Nothing AndAlso j.ObjectSource.ObjectAlias Is al Then
                        If Not _includeEntities.Contains(j.ObjectSource) Then
                            _includeEntities.Add(j.ObjectSource)
                        End If
                        Return Me
                    End If
                Next
                'Throw New QueryCmdException("Entity must be present among joins", Me)
            End If

            Return Me
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

        'Public Shared Function CreateAndGetOrmCommand(Of T As {New, _IKeyEntity})(ByVal mgr As OrmManager) As OrmQueryCmd(Of T)
        '    Dim selectType As Type = GetType(T)
        '    Return Create(mgr).GetOrmCommand(Of T)(mgr)
        'End Function

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

        'Public Shared Function CreateAndGetOrmCommand(Of T As {New, _IKeyEntity})(ByVal name As String, ByVal mgr As OrmManager) As OrmQueryCmd(Of T)
        '    Dim selectType As Type = GetType(T)
        '    Return Create(name, mgr).GetOrmCommand(Of T)(mgr)
        'End Function

        Public Shared Function Create(ByVal name As String, ByVal mgr As OrmManager) As QueryCmd
            Dim f As ICreateQueryCmd = TryCast(mgr, ICreateQueryCmd)
            Dim q As QueryCmd = Nothing
            If f Is Nothing Then
                q = New QueryCmd With {
                    .Name = name
                }
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

        Public Shared Function Create() As QueryCmd
            Return Create(OrmManager.CurrentManager)
        End Function

        'Public Shared Function CreateAndGetOrmCommand(Of T As {New, _IKeyEntity})() As OrmQueryCmd(Of T)
        '    Return CreateAndGetOrmCommand(Of T)(OrmManager.CurrentManager)
        'End Function

        'Public Shared Function Create(ByVal selectType As Type) As QueryCmd
        '    Return Create(selectType, OrmManager.CurrentManager)
        'End Function

        'Public Shared Function CreateByEntityName(ByVal entityName As String) As QueryCmd
        '    Return CreateByEntityName(entityName, OrmManager.CurrentManager)
        'End Function

        'Public Shared Function Create(ByVal name As String, ByVal table As SourceFragment) As QueryCmd
        '    Return Create(name, table, OrmManager.CurrentManager)
        'End Function

        'Public Shared Function CreateAndGetOrmCommand(Of T As {New, _IKeyEntity})(ByVal name As String) As OrmQueryCmd(Of T)
        '    Return CreateAndGetOrmCommand(Of T)(name, OrmManager.CurrentManager)
        'End Function

        Public Shared Function Create(ByVal name As String) As QueryCmd
            Return Create(name, OrmManager.CurrentManager)
        End Function

        'Public Shared Function CreateByEntityName(ByVal name As String, ByVal entityName As String) As QueryCmd
        '    Return CreateByEntityName(name, entityName, OrmManager.CurrentManager)
        'End Function

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

        Public Shared Function Search(ByVal entityName As String, ByVal searchText As String, ByVal getMgr As CreateManagerDelegate) As QueryCmd
            Dim q As New QueryCmd(New CreateManager(getMgr))
            q.From(New SearchFragment(New EntityUnion(entityName), searchText))
            Return q
        End Function

        Public Shared Function Search(ByVal entityName As String, ByVal searchText As String, ByVal getMgr As ICreateManager) As QueryCmd
            Dim q As New QueryCmd(getMgr)
            q.From(New SearchFragment(New EntityUnion(entityName), searchText))
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
            GetDynamicKey(sb, Nothing)
            Return sb.ToString
        End Function

        Public Function GetStaticString(ByVal mpe As ObjectMappingEngine) As String Implements Criteria.Values.IQueryElement.GetStaticString
            Return ToStaticString(mpe)
        End Function

#Region " GetByID "
        Public Function [GetByID](Of T As {New, ISinglePKEntity})(ByVal id As Object, ByVal options As GetByIDOptions) As T
            Using gm = GetManager(_getMgr)
                If gm Is Nothing Then
                    Throw New QueryCmdException("OrmManager required", Me)
                End If

                Return GetByID(Of T)(id, options, gm.Manager)
            End Using
        End Function

        Public Function [GetByID](Of T As {New, ISinglePKEntity})(ByVal id As Object) As T
            Return GetByID(Of T)(id, GetByIDOptions.EnsureExistsInStore)
        End Function

        Public Function [GetByID](Of T As {New, ISinglePKEntity})(ByVal id As Object, ByVal mgr As OrmManager) As T
            Return GetByID(Of T)(id, GetByIDOptions.EnsureExistsInStore, mgr)
        End Function

        Public Function [GetByID](Of T As {New, ISinglePKEntity})(ByVal id As Object, ByVal options As GetByIDOptions, ByVal mgr As OrmManager) As T
            If mgr Is Nothing Then
                Throw New QueryCmdException("Manager is required", Me)
            End If

            'Dim f As IGetFilter = Nothing
            'Dim pk As String = Nothing

            Dim selou As EntityUnion = GetSelectedOS()
            Dim tp As Type = Nothing
            If selou IsNot Nothing Then
                tp = selou.GetRealType(mgr.MappingEngine)
                '[Select](selou, ensureLoaded)
                'f = Ctor.prop(selou, mgr.MappingEngine.GetPrimaryKeys(tp)(0).PropertyAlias).eq(id)
            Else
                tp = GetType(T)
                '[Select](tp, ensureLoaded)
                'f = Ctor.prop(GetType(T), mgr.MappingEngine.GetPrimaryKeys(GetType(T))(0).PropertyAlias).eq(id)
            End If

            'If mgr.IsInCache(id, tp) Then
            '    Dim o As IKeyEntity = mgr.LoadType(id, tp, ensureLoaded, False)
            '    If o IsNot Nothing Then
            '        If _getMgr IsNot Nothing Then
            '            o.SetCreateManager(_getMgr)
            '        End If
            '        Return CType(o, T)
            '    End If
            'End If

            'Return Where(f).Single(Of T)()

            Dim o As ISinglePKEntity = Nothing
            Using New SetManagerHelper(mgr, CreateManager, _schema, ContextInfo)
                Dim oldSch As ObjectMappingEngine = mgr.MappingEngine
                If SpecificMappingEngine IsNot Nothing AndAlso Not oldSch.Equals(SpecificMappingEngine) Then
                    mgr.SetMapping(SpecificMappingEngine)
                End If

                Try
                    Using GetExecutor(mgr).SubscribeToErrorHandling(mgr, Me)

                        If GetType(T) IsNot tp Then
                            Select Case options
                                Case GetByIDOptions.EnsureExistsInStore
                                    o = mgr.GetKeyEntityFromCacheOrDB(id, tp)
                                    If o IsNot Nothing AndAlso o.ObjectState = ObjectState.NotFoundInSource Then
                                        o = Nothing
                                    End If
                                Case GetByIDOptions.GetAsIs
                                    o = mgr.GetKeyEntityFromCacheOrCreate(id, tp)
                                Case GetByIDOptions.EnsureLoadedFromStore
                                    o = mgr.GetKeyEntityFromCacheLoadedOrDB(id, tp)
                                    If o IsNot Nothing AndAlso o.ObjectState = ObjectState.NotFoundInSource Then
                                        o = Nothing
                                    End If
                                Case Else
                                    Throw New NotImplementedException
                            End Select
                        Else
                            'o = mgr.LoadType(Of T)(id, ensureLoaded, False)
                            Select Case options
                                Case GetByIDOptions.EnsureExistsInStore
                                    o = mgr.GetKeyEntityFromCacheOrDB(Of T)(id)
                                    If o IsNot Nothing AndAlso o.ObjectState = ObjectState.NotFoundInSource Then
                                        o = Nothing
                                    End If
                                Case GetByIDOptions.GetAsIs
                                    o = mgr.GetKeyEntityFromCacheOrCreate(Of T)(id)
                                Case GetByIDOptions.EnsureLoadedFromStore
                                    o = mgr.GetKeyEntityFromCacheLoadedOrDB(Of T)(id)
                                    If o IsNot Nothing AndAlso o.ObjectState = ObjectState.NotFoundInSource Then
                                        o = Nothing
                                    End If
                                Case Else
                                    Throw New NotImplementedException
                            End Select
                        End If
                    End Using
                Finally
                    'UnsubscribeFromCommandEvents(Query, dbm, cmdHandler)
                    'UnsubscribeFromConnectionEvents(Query, dbm, connHandler)

                    mgr.SetMapping(oldSch)
                End Try
            End Using

            If o IsNot Nothing Then
                If o.GetICreateManager Is Nothing AndAlso _getMgr IsNot Nothing Then
                    o.SetCreateManager(_getMgr)
                End If
                If o.SpecificMappingEngine Is Nothing Then
                    o.SpecificMappingEngine = SpecificMappingEngine
                End If
            End If

            Return CType(o, T)
        End Function

        'Friend Sub ConvertIdsToObjects(Of T As {New, ISinglePKEntity})(ByVal rt As Type, ByVal list As IListEdit, _
        '    ByVal ids As IEnumerable(Of Object), ByVal mgr As OrmManager)
        '    For Each id As Object In ids
        '        Dim obj As T = mgr.GetKeyEntityFromCacheOrCreate(Of T)(id, True)

        '        If obj IsNot Nothing Then
        '            list.Add(obj)
        '        ElseIf mgr.Cache.NewObjectManager IsNot Nothing Then
        '            obj = CType(mgr.Cache.NewObjectManager.GetNew(rt, OrmManager.GetPKValues(obj, Nothing)), T)
        '            If obj IsNot Nothing Then list.Add(obj)
        '        End If
        '    Next
        'End Sub

        'Friend Sub ConvertIdsToObjects(ByVal rt As Type, ByVal list As IListEdit, _
        '    ByVal ids As IEnumerable(Of Object), ByVal mgr As OrmManager)
        '    For Each id As Object In ids
        '        Dim obj As ISinglePKEntity = mgr.GetKeyEntityFromCacheOrCreate(id, rt, True)

        '        If obj IsNot Nothing Then
        '            list.Add(obj)
        '        ElseIf mgr.Cache.NewObjectManager IsNot Nothing Then
        '            obj = CType(mgr.Cache.NewObjectManager.GetNew(rt, OrmManager.GetPKValues(obj, Nothing)), ISinglePKEntity)
        '            If obj IsNot Nothing Then list.Add(obj)
        '        End If
        '    Next
        'End Sub

        Public Function [GetByIds](Of T As {New, ISinglePKEntity})( _
                    ByVal ids As IEnumerable(Of Object), _
                    ByVal options As GetByIDOptions, _
                    ByVal mgr As OrmManager) As ReadOnlyList(Of T)

            If mgr Is Nothing Then Throw New ArgumentNullException("Manager is required")

            Dim tp As Type = GetRealType(Of T)(mgr)
            Dim ro As New ReadOnlyList(Of T)
            Dim list As IListEdit = ro

            Using New SetManagerHelper(mgr, CreateManager, _schema, ContextInfo)
                Dim oldSch As ObjectMappingEngine = mgr.MappingEngine
                If SpecificMappingEngine IsNot Nothing AndAlso Not oldSch.Equals(SpecificMappingEngine) Then
                    mgr.SetMapping(SpecificMappingEngine)
                End If

                Try
                    Select Case options
                        Case GetByIDOptions.GetAsIs
                            If GetType(T) IsNot tp Then
                                DataContext.ConvertIdsToObjects(Of T)(tp, list, ids, mgr)
                            Else
                                DataContext.ConvertIdsToObjects(tp, list, ids, mgr)
                            End If
                        Case GetByIDOptions.EnsureExistsInStore
                            If GetType(T) IsNot tp Then
                                DataContext.ConvertIdsToObjects(Of T)(tp, list, ids, mgr)
                            Else
                                DataContext.ConvertIdsToObjects(tp, list, ids, mgr)
                            End If
                            ro = CType(Query.QueryCmd.LoadObjects(ro, 0, list.Count, False, mgr), ReadOnlyList(Of T))
                        Case GetByIDOptions.EnsureLoadedFromStore
                            If GetType(T) IsNot tp Then
                                DataContext.ConvertIdsToObjects(Of T)(tp, list, ids, mgr)
                            Else
                                DataContext.ConvertIdsToObjects(tp, list, ids, mgr)
                            End If
                            ro = CType(Query.QueryCmd.LoadObjects(ro, 0, list.Count, True, mgr), ReadOnlyList(Of T))
                        Case Else
                            Throw New NotImplementedException
                    End Select
                    'Else
                    'Select Case options
                    '    Case GetByIDOptions.GetAsIs
                    '        ConvertIdsToObjects(tp, list, ids, mgr)
                    '    Case GetByIDOptions.EnsureExistsInStore
                    '        ConvertIdsToObjects(tp, list, ids, mgr)
                    '        ro = mgr.LoadObjects(Of T)(ro, 0, list.Count, mgr.MappingEngine.GetPrimaryKeys(tp))
                    '    Case GetByIDOptions.EnsureLoadedFromStore
                    '        ConvertIdsToObjects(tp, list, ids, mgr)
                    '        Dim props As New List(Of EntityPropertyAttribute)()
                    '        For Each ep As EntityPropertyAttribute In mgr.MappingEngine.GetProperties(tp).Keys
                    '            props.Add(ep)
                    '        Next
                    '        ro = mgr.LoadObjects(Of T)(ro, 0, list.Count, props)
                    '    Case Else
                    '        Throw New NotImplementedException
                    'End Select
                    'End If

                    For Each o As ISinglePKEntity In list
                        If o IsNot Nothing Then
                            If o.GetICreateManager Is Nothing AndAlso _getMgr IsNot Nothing Then
                                o.SetCreateManager(_getMgr)
                            End If
                            If o.SpecificMappingEngine Is Nothing Then
                                o.SpecificMappingEngine = SpecificMappingEngine
                            End If
                        End If
                    Next
                Finally
                    mgr.SetMapping(oldSch)
                End Try
            End Using

            Return ro
        End Function

        Public Function [GetByIds](Of T As {New, ISinglePKEntity})( _
                            ByVal ids As IEnumerable(Of Object), _
                            ByVal options As GetByIDOptions) As ReadOnlyList(Of T)

            Using gm = GetManager(_getMgr)
                If gm Is Nothing Then
                    Throw New QueryCmdException("OrmManager required", Me)
                End If

                Return GetByIds(Of T)(ids, options, gm.Manager)
            End Using

        End Function

        'Public Function [GetByIds](Of T As {New, ISinglePKEntity})( _
        '    ByVal ids As ICollection(Of Object), _
        '    ByVal options As GetByIDOptions, mgr As OrmManager) As ReadOnlyList(Of T)
        '    Return GetByIds(Of T)(ids, options, mgr)
        'End Function

        Public Function [GetByIds](Of T As {New, ISinglePKEntity})(ByVal ids As IEnumerable(Of Object)) As ReadOnlyList(Of T)
            Return GetByIds(Of T)(ids, GetByIDOptions.GetAsIs)
        End Function

        Public Function [GetByIds](Of T As {New, ISinglePKEntity})(ByVal ids As IEnumerable(Of Object), ByVal mgr As OrmManager) As ReadOnlyList(Of T)
            Return GetByIds(Of T)(ids, GetByIDOptions.GetAsIs, mgr)
        End Function

        Private Function GetRealType(Of T As {New, ISinglePKEntity})(ByVal mgr As OrmManager) As Type
            If mgr Is Nothing Then
                Throw New ArgumentNullException("mgr")
            End If

            Dim selou As EntityUnion = GetSelectedOS()
            Dim tp As Type = Nothing
            If selou IsNot Nothing Then
                tp = selou.GetRealType(mgr.MappingEngine)
            Else
                tp = GetType(T)
            End If

            Return tp
        End Function

        Public Function [GetByIDDyn](Of T As {ISinglePKEntity})(ByVal id As Object, ByVal options As GetByIDOptions) As T
            Using gm = GetManager(_getMgr)
                If gm Is Nothing Then
                    Throw New QueryCmdException("OrmManager required", Me)
                End If

                Return GetByIDDyn(Of T)(id, options, gm.Manager)
            End Using
        End Function

        Public Function [GetByIDDyn](Of T As {ISinglePKEntity})(ByVal id As Object) As T
            Return GetByIDDyn(Of T)(id, GetByIDOptions.GetAsIs)
        End Function

        Public Function [GetByIDDyn](Of T As {ISinglePKEntity})(ByVal id As Object, ByVal options As GetByIDOptions, ByVal mgr As OrmManager) As T
            If mgr Is Nothing Then
                Throw New QueryCmdException("Manager is required", Me)
            End If

            'Dim f As IGetFilter = Nothing
            'Dim pk As String = Nothing

            Dim selou As EntityUnion = GetSelectedOS()
            Dim tp As Type = selou.GetRealType(mgr.MappingEngine)

            Dim o As ISinglePKEntity = Nothing
            Using New SetManagerHelper(mgr, CreateManager, _schema, ContextInfo)
                Dim oldSch As ObjectMappingEngine = mgr.MappingEngine
                If SpecificMappingEngine IsNot Nothing AndAlso Not oldSch.Equals(SpecificMappingEngine) Then
                    mgr.SetMapping(SpecificMappingEngine)
                End If
                Try
                    Using GetExecutor(mgr).SubscribeToErrorHandling(mgr, Me)
                        Select Case options
                            Case GetByIDOptions.EnsureExistsInStore
                                o = mgr.GetKeyEntityFromCacheOrDB(id, tp)
                            Case GetByIDOptions.GetAsIs
                                o = mgr.GetKeyEntityFromCacheOrCreate(id, tp)
                            Case GetByIDOptions.EnsureLoadedFromStore
                                o = mgr.GetKeyEntityFromCacheLoadedOrDB(id, tp)
                            Case Else
                                Throw New NotImplementedException
                        End Select
                    End Using
                Finally
                    mgr.SetMapping(oldSch)
                End Try
            End Using

            If o IsNot Nothing Then
                If o.GetICreateManager Is Nothing AndAlso _getMgr IsNot Nothing Then
                    o.SetCreateManager(_getMgr)
                End If
                If o.SpecificMappingEngine Is Nothing Then
                    o.SpecificMappingEngine = SpecificMappingEngine
                End If
            End If


            Return CType(o, T)
        End Function
#End Region

        Protected Function BuildDic(Of T As {New, _IEntity})(ByVal mgr As OrmManager, _
            ByVal firstPropertyAlias As String, ByVal secondPropertyAlias As String, ByVal level As Integer) As DicIndexT(Of T)

            Dim last As DicIndexT(Of T) = DicIndexT(Of T).CreateRoot(firstPropertyAlias, secondPropertyAlias, Me)
            Dim root As DicIndexT(Of T) = last
            Dim first As Boolean = True

            Dim pn As String = SelectList(0).GetIntoPropertyAlias
            'If String.IsNullOrEmpty(pn) Then
            '    pn = SelectList(0).PropertyAlias
            'End If
            Dim cn As String = SelectList(1).GetIntoPropertyAlias
            'If String.IsNullOrEmpty(cn) Then
            '    cn = SelectList(1).PropertyAlias
            'End If

            For Each a As AnonymousEntity In ToAnonymList(mgr)
                OrmManager.BuildDic(Of DicIndexT(Of T), T)(CStr(a(pn)), CInt(a(cn)), level, root, last, first, firstPropertyAlias, secondPropertyAlias)
            Next

            Return root
        End Function

        Public Function BuildDictionary(Of T As {New, _IEntity})(ByVal level As Integer) As DicIndexT(Of T)
            Return BuildDictionary(Of T)(_getMgr, level)
        End Function

        Public Function BuildDictionary(Of T As {New, _IEntity})(ByVal getMgr As ICreateManager, ByVal level As Integer) As DicIndexT(Of T)
            Using gm = GetManager(getMgr)
                If gm Is Nothing Then
                    Throw New QueryCmdException("OrmManager required", Me)
                End If

                Using New SetManagerHelper(gm.Manager, getMgr, _schema, ContextInfo)
                    Return BuildDictionary(Of T)(gm.Manager, level)
                End Using
            End Using
        End Function

        Public Function BuildDictionary(Of T As {New, _IEntity})(ByVal propertyAlias As String, ByVal level As Integer) As DicIndexT(Of T)
            Return BuildDictionary(Of T)(_getMgr, propertyAlias, level)
        End Function

        Public Function BuildDictionary(Of T As {New, _IEntity})(ByVal propertyAlias As String, ByVal secondPropertyAlias As String, ByVal level As Integer) As DicIndexT(Of T)
            Return BuildDictionary(Of T)(_getMgr, propertyAlias, secondPropertyAlias, level)
        End Function

        Public Function BuildDictionary(Of T As {New, _IEntity})(ByVal getMgr As ICreateManager, ByVal propertyAlias As String, ByVal level As Integer) As DicIndexT(Of T)
            Using gm = GetManager(getMgr)
                If gm Is Nothing Then
                    Throw New QueryCmdException("OrmManager required", Me)
                End If

                Using New SetManagerHelper(gm.Manager, getMgr, _schema, ContextInfo)
                    Return BuildDictionary(Of T)(gm.Manager, propertyAlias, level)
                End Using
            End Using
        End Function

        Public Function BuildDictionary(Of T As {New, _IEntity})(ByVal getMgr As ICreateManager, _
            ByVal propertyAlias As String, ByVal secondPropertyAlias As String, ByVal level As Integer) As DicIndexT(Of T)
            Using gm = GetManager(getMgr)
                Using New SetManagerHelper(gm.Manager, getMgr, _schema, ContextInfo)
                    Return BuildDictionary(Of T)(gm.Manager, propertyAlias, secondPropertyAlias, level)
                End Using
            End Using
        End Function

        Public Function BuildDictionary(Of T As {New, _IEntity})(ByVal mgr As OrmManager, ByVal level As Integer) As DicIndexT(Of T)
            If mgr Is Nothing Then
                Throw New ArgumentNullException("mgr")
            End If

            If _group Is Nothing Then
                Dim tt As Type = GetType(T)
                If _from IsNot Nothing AndAlso _from.ObjectSource IsNot Nothing Then
                    tt = _from.ObjectSource.GetRealType(mgr.MappingEngine)
                End If

                Dim oschema As IEntitySchema = mgr.MappingEngine.GetEntitySchema(tt)
                Dim idic As ISupportAlphabet = TryCast(oschema, ISupportAlphabet)

                If idic IsNot Nothing Then
                    Return BuildDictionary(Of T)(mgr, idic.GetFirstDicField, idic.GetSecondDicField, level)
                Else
                    Throw New InvalidOperationException("Group clause not specified")
                End If
            End If

            Dim n As String = Nothing

            For Each e As Expressions2.IExpression In _group.GetExpressions
                Dim se As EntityExpression = TryCast(e, EntityExpression)
                If se IsNot Nothing Then
                    n = se.ObjectProperty.PropertyAlias
                    Exit For
                End If
            Next

            If String.IsNullOrEmpty(n) Then
                Throw New QueryCmdException("Cannot get property alias from group expression: " & _group.GetDynamicString, Me)
            End If

            Return BuildDic(Of T)(mgr, n, Nothing, level)
        End Function

        Public Function BuildDictionary(Of T As {New, _IEntity})(ByVal mgr As OrmManager, ByVal propertyAlias As String, ByVal level As Integer) As DicIndexT(Of T)
            If mgr Is Nothing Then
                Throw New ArgumentNullException("mgr")
            End If

            Dim tt As Type = GetType(T)
            Dim c As New QueryCmd.svct(Me)
            Using New OnExitScopeAction(AddressOf c.SetCT2Nothing)
                Dim g As GroupExpression = _group

                Dim srt As OrderByClause = _order

                Dim so As EntityUnion = GetSelectedOS()

                If _from IsNot Nothing AndAlso _from.ObjectSource IsNot Nothing Then
                    tt = _from.ObjectSource.GetRealType(mgr.MappingEngine)
                ElseIf _from Is Nothing Then
                    If so IsNot Nothing Then
                        _from = New FromClauseDef(GetSelectedOS)
                    Else
                        _from = New FromClauseDef(tt)
                    End If
                End If

                Dim s As SelectExpression = Nothing
                If so IsNot Nothing Then
                    Dim a As IGetExpression = ECtor.prop(so, propertyAlias)
                    s = FCtor.custom(String.Format(mgr.StmtGenerator.Left, "{0}", level), a).into("Pref")
                Else
                    s = FCtor.custom(String.Format(mgr.StmtGenerator.Left, "{0}", level), ECtor.prop(tt, propertyAlias)).into("Pref")
                End If

                Try
                    [Select](FCtor _
                             .Exp(s) _
                             .count().into("Count")) _
                    .GroupBy(GCtor.Exp(s)) _
                    .OrderBy(SCtor.count().desc)

                    Return BuildDictionary(Of T)(mgr, level)
                Finally
                    _group = g
                    _order = srt
                End Try
            End Using
        End Function

        Public Function BuildDictionary(Of T As {New, _IEntity})(ByVal mgr As OrmManager, _
            ByVal firstPropertyAlias As String, ByVal secondPropertyAlias As String, ByVal level As Integer) As DicIndexT(Of T)
            If mgr Is Nothing Then
                Throw New ArgumentNullException("mgr")
            End If

            If Not String.IsNullOrEmpty(secondPropertyAlias) Then
                Dim selEU As EntityUnion = GetSelectedOS()
                If selEU Is Nothing Then
                    Dim tt As Type = GetType(T)

                    If _from IsNot Nothing AndAlso _from.ObjectSource IsNot Nothing Then
                        tt = _from.ObjectSource.GetRealType(mgr.MappingEngine)
                    End If

                    selEU = New EntityUnion(tt)
                End If

                Dim s1 As SelectExpression = FCtor.custom( _
                    String.Format(mgr.StmtGenerator.Left, "{0}", level), ECtor.prop(selEU, firstPropertyAlias)).into("Pref")
                Dim s2 As SelectExpression = FCtor.custom( _
                    String.Format(mgr.StmtGenerator.Left, "{0}", level), ECtor.prop(selEU, secondPropertyAlias)).into("Pref")

                'Dim c As New QueryCmd.svct(Me)
                'Using New OnExitScopeAction(AddressOf c.SetCT2Nothing)

                '    Dim g As ObjectModel.ReadOnlyCollection(Of Grouping) = Nothing
                '    If _group IsNot Nothing Then
                '        g = New ObjectModel.ReadOnlyCollection(Of Grouping)(_group)
                '    End If
                '    Dim srt As Sort = Nothing
                '    If _order IsNot Nothing Then
                '        srt = _order
                '    End If

                '    Try
                'From( _
                '    New QueryCmd() _
                '        .[Select](FCtor.Exp(s1).count("Count")) _
                '        .From(tt) _
                '        .GroupBy(FCtor.Exp(s1)) _
                '        .UnionAll( _
                '    New QueryCmd() _
                '        .[Select](FCtor.Exp(s2).count("Count")) _
                '        .From(tt) _
                '        .GroupBy(FCtor.Exp(s2)) _
                '    ) _
                ') _
                Dim f As FromClauseDef = FromClause
                If f Is Nothing Then
                    f = New FromClauseDef(selEU)
                End If

                Dim q As New QueryCmd(_getMgr) With {
                    .SpecificMappingEngine = SpecificMappingEngine
                }
                q.From(CType(Clone(), QueryCmd) _
                     .[Select](FCtor.Exp(s1).count().into("Count")) _
                     .From(f) _
                     .GroupBy(GCtor.Exp(s1)) _
                     .UnionAll( _
                     CType(Clone(), QueryCmd) _
                     .[Select](FCtor.Exp(s2).count().into("Count")) _
                     .From(f) _
                     .GroupBy(GCtor.Exp(s2)) _
                     ) _
                ) _
                .Select(FCtor.prop("Pref").sum("Count").into("Count")) _
                .GroupBy(GCtor.prop("Pref")) _
                .OrderBy(SCtor.prop("Pref").desc)

                Return q.BuildDic(Of T)(mgr, firstPropertyAlias, secondPropertyAlias, level)
                '    Finally
                '    Group = g
                '    _order = srt
                'End Try
                'End Using
            Else
                Return BuildDictionary(Of T)(mgr, firstPropertyAlias, level)
            End If
        End Function

        Public Function ContainsDyn(Of T As _ICachedEntity)(ByVal o As T) As Boolean
            Dim tt As Type = Nothing
            If SelectedEntities IsNot Nothing AndAlso SelectedEntities.Count = 1 Then
                Dim eu As EntityUnion = SelectedEntities(0).First
                tt = eu.GetRealType(GetMappingEngine)
                Dim oldf As IGetFilter = Filter
                Try
                    WhereAnd(Ctor.prop(eu, GetMappingEngine.GetPrimaryKey(tt)).eq(o))
                    Return SingleOrDefaultDyn(Of T)() IsNot Nothing
                Finally
                    _filter = oldf
                End Try
            Else
                Throw New QueryCmdException("QueryCmd doesn't select entity", Me)
            End If
        End Function

        Public Function Contains(Of T As {New, _ICachedEntity})(ByVal o As T) As Boolean
            Dim tt As Type = Nothing
            If SelectedEntities IsNot Nothing AndAlso SelectedEntities.Count = 1 Then
                Dim eu As EntityUnion = SelectedEntities(0).First
                tt = eu.GetRealType(GetMappingEngine)
                Dim oldf As IGetFilter = Filter
                Try
                    WhereAnd(Ctor.prop(eu, GetMappingEngine.GetPrimaryKey(tt)).eq(o))
                    Return SingleOrDefault(Of T)() IsNot Nothing
                Finally
                    _filter = oldf
                End Try
            Else
                Throw New QueryCmdException("QueryCmd doesn't select entity", Me)
            End If
        End Function

        Public Function GetEntitySchema(ByVal mpe As ObjectMappingEngine, ByVal t As System.Type) As Entities.Meta.IEntitySchema Implements IExecutionContext.GetEntitySchema
            Dim oschema As IEntitySchema = mpe.GetEntitySchema(t, False)

            If oschema Is Nothing AndAlso _poco IsNot Nothing Then
                oschema = CType(_poco(t), IEntitySchema)
            End If

            If oschema Is Nothing Then
                Throw New QueryCmdException(String.Format("Object schema for type {0} is not defined", t), Me)
            End If

            Return oschema
        End Function

        Public Sub ReplaceSchema(ByVal mpe As ObjectMappingEngine, ByVal t As System.Type, ByVal newMap As Collections.IndexedCollection(Of String, MapField2Column)) Implements IExecutionContext.ReplaceSchema
            If _newMaps Is Nothing Then
                _newMaps = Hashtable.Synchronized(New Hashtable)
            End If
            _newMaps(t) = newMap
        End Sub

        Public Function GetFieldColumnMap(ByVal oschema As Entities.Meta.IEntitySchema, ByVal t As System.Type) As Collections.IndexedCollection(Of String, MapField2Column) Implements IExecutionContext.GetFieldColumnMap
            If _newMaps IsNot Nothing AndAlso _newMaps.Contains(t) Then
                Return CType(_newMaps(t), Collections.IndexedCollection(Of String, MapField2Column))
            End If
            Return oschema.FieldColumnMap
        End Function

        Public Sub SetCache(ByVal l As ICollection)
            Using gm = GetManager(_getMgr)
                If gm Is Nothing Then
                    Throw New QueryCmdException("OrmManager required", Me)
                End If

                SetCache(gm.Manager, l)
            End Using
        End Sub

        Public Sub SetCache(ByVal mgr As OrmManager, ByVal l As ICollection)
            CType(GetExecutor(mgr), QueryExecutor).SetCache(mgr, Me, l)
        End Sub

        Protected Friend Function RaiseModifyResult(ByVal mgr As OrmManager, ByVal m As ReadonlyMatrix, ByVal ci As Object, fromCache As Boolean) As ModifyResultArgs
            Dim r As New ModifyResultArgs(mgr, fromCache) With {.Matrix = m, .CustomInfo = ci}
            RaiseEvent ModifyResult(Me, r)
            Return r
        End Function

        Protected Friend Function RaiseModifyResult(ByVal mgr As OrmManager, ByVal l As IReadOnlyList, ByVal ci As Object, fromCache As Boolean) As ModifyResultArgs
            Dim r As New ModifyResultArgs(mgr, fromCache) With {.ReadOnlyList = l, .CustomInfo = ci}
            RaiseEvent ModifyResult(Me, r)
            Return r
        End Function

        Protected Friend Function RaiseModifyResult(ByVal mgr As OrmManager, ByVal l As ICollection, ByVal ci As Object, fromCache As Boolean) As ModifyResultArgs
            Dim r As New ModifyResultArgs(mgr, fromCache) With {.SimpleList = l, .CustomInfo = ci}
            RaiseEvent ModifyResult(Me, r)
            Return r
        End Function

        Protected Friend Function RaiseModifyResult(ByVal mgr As OrmManager, ByVal m As ReadonlyMatrix, fromCache As Boolean) As ModifyResultArgs
            Dim r As New ModifyResultArgs(mgr, fromCache) With {.Matrix = m}
            RaiseEvent ModifyResult(Me, r)
            Return r
        End Function

        Protected Friend Function RaiseModifyResult(ByVal mgr As OrmManager, ByVal l As IReadOnlyList, fromCache As Boolean) As ModifyResultArgs
            Dim r As New ModifyResultArgs(mgr, fromCache) With {.ReadOnlyList = l}
            RaiseEvent ModifyResult(Me, r)
            Return r
        End Function

        Protected Friend Function RaiseModifyResult(ByVal mgr As OrmManager, ByVal l As ICollection, fromCache As Boolean) As ModifyResultArgs
            Dim r As New ModifyResultArgs(mgr, fromCache) With {.SimpleList = l}
            RaiseEvent ModifyResult(Me, r)
            Return r
        End Function

        Property DontUseErrorHandlingInManager As Boolean

        Friend Function RaiseConnectionErrorEvent(args As ConnectionExceptionArgs) As ConnectionExceptionEventHandler
            RaiseEvent ConnectionException(Me, args)
            If Not DontUseErrorHandlingInManager Then
                Return ConnectionExceptionEvent
            End If
            Return Nothing
        End Function

        Friend Function HasConnectionErrorSubscribers() As Boolean
            Return ConnectionExceptionEvent IsNot Nothing
        End Function

        Friend Function RaiseCommandErrorEvent(args As CommandExceptionArgs) As CommandExceptionEventHandler
            RaiseEvent CommandException(Me, args)
            If Not DontUseErrorHandlingInManager Then
                Return CommandExceptionEvent
            End If
            Return Nothing
        End Function

        Friend Function HasCommandErrorSubscribers() As Boolean
            Return CommandExceptionEvent IsNot Nothing
        End Function

        Friend Function ContainsConnectionExceptionSubscriber(qh As ConnectionExceptionEventHandler) As Boolean
            Return ConnectionExceptionEvent IsNot Nothing AndAlso Array.IndexOf(ConnectionExceptionEvent.GetInvocationList, qh) >= 0
        End Function

        Friend Function ContainsCommandExceptionSubscriber(qh As CommandExceptionEventHandler) As Boolean
            Return CommandExceptionEvent IsNot Nothing AndAlso Array.IndexOf(CommandExceptionEvent.GetInvocationList, qh) >= 0
        End Function

        Public Overridable Property IncludeModifiedObjects As Boolean
            Get
                Return Array.IndexOf(ModifyResultEvent.GetInvocationList, CType(AddressOf _ModifyResult, ModifyResultEventHandler)) >= 0
            End Get
            Set(value As Boolean)
                If value Then
                    AddHandler ModifyResult, AddressOf _ModifyResult
                Else
                    RemoveHandler ModifyResult, AddressOf _ModifyResult
                End If
            End Set
        End Property

        Public Property FallBack As OrmManager.ApplyFilterFallBackDelegate
            Get
                Return _fallBack
            End Get
            Set(value As OrmManager.ApplyFilterFallBackDelegate)
                _fallBack = value
            End Set
        End Property

        Protected Overridable Sub _ModifyResult(ByVal sender As QueryCmd, ByVal args As ModifyResultArgs)
            If args.OrmManager.Cache.NewObjectManager IsNot Nothing AndAlso Not args.IsSimple Then
                Dim removed As Boolean = False
                Dim nr As IListEdit = Nothing
                For Each o As IEntity In args.ReadOnlyList
                    If o.ObjectState = ObjectState.Deleted Then
                        If nr Is Nothing Then
                            nr = CType(args.ReadOnlyList.Clone, IListEdit)
                        End If
                        nr.Remove(o)
                        removed = True
                    End If
                Next

                Dim toAdd As New List(Of IEntity)
                Dim nex As INewObjectsStoreEx = TryCast(args.OrmManager.Cache.NewObjectManager, INewObjectsStoreEx)
                If nex IsNot Nothing Then
                    Dim s As IList(Of _ICachedEntity) = nex.GetNewObjects(args.ReadOnlyList.RealType)
                    If s IsNot Nothing AndAlso s.Count > 0 Then
                        If nr Is Nothing Then
                            nr = CType(args.ReadOnlyList.Clone, IListEdit)
                        End If
                        'Dim objEU = GetSelectedOS()
                        Dim objEU As New EntityUnion(args.ReadOnlyList.RealType)
                        For Each a As IEntity In args.OrmManager.ApplyFilter(args.ReadOnlyList.RealType, s, _f, _js, objEU, _fallBack)
                            If nr Is Nothing OrElse Not nr.Contains(a) Then
                                toAdd.Add(a)
                            End If
                        Next
                    End If
                End If

                If nr Is Nothing OrElse (nr.Count = 0 AndAlso toAdd.Count = 0 AndAlso Not removed) Then
                    Return
                End If

                If Sort IsNot Nothing Then
                    Dim c As New Sorting.EntityComparer(Sort)
                    Dim newres As ArrayList = ArrayList.Adapter(nr)
                    For Each o As IEntity In toAdd
                        Dim pos As Integer = newres.BinarySearch(o, c)
                        If pos < 0 Then
                            nr.Insert(Not pos, o)
                        Else
                            nr.Insert(pos, o)
                            'Throw New QueryCmdException("Object in added list already in query", Me)
                        End If
                    Next
                    If TopParam IsNot Nothing Then
                        Dim cnt As Integer = nr.Count
                        For i As Integer = TopParam.Count To cnt - 1
                            nr.RemoveAt(i)
                        Next
                    End If
                Else
                    For i As Integer = 0 To toAdd.Count - 1
                        If TopParam IsNot Nothing Then
                            If TopParam.Count + i <= nr.Count Then
                                Exit For
                            End If
                        End If
                        nr.Add(toAdd(i))
                    Next
                End If

                args.ReadOnlyList = nr
            End If
        End Sub

        '    Public Function MakeQueryStatement(mgr As OrmManager, ByVal params As ICreateParam) As String
        '        If Not _prepared Then
        '            QueryCmd.Prepare(Me, GetExecutor(mgr), mgr.MappingEngine, mgr.GetContextInfo, mgr.StmtGenerator)
        '            If _cancel Then
        '                Return Nothing
        '            End If
        '        End If

        '        Return db
        '    End Function

    End Class

    '    Public Class OrmQueryCmd(Of T As {New, _IKeyEntity})
    '        Inherits QueryCmd
    '        Implements Generic.IEnumerable(Of T)

    '        Private _preCmp As ReadOnlyList(Of T)
    '        Private _oldMark As Guid

    '        Public Sub Exec(ByVal mgr As OrmManager)
    '            _preCmp = ToOrmListDyn(Of T)(mgr)
    '            _oldMark = _mark
    '        End Sub

    '        Public Shared Widening Operator CType(ByVal cmd As OrmQueryCmd(Of T)) As ReadOnlyList(Of T)
    '            If cmd._getMgr IsNot Nothing Then
    '                Return cmd.ToOrmList(Of T, T)(cmd._getMgr)
    '            ElseIf cmd._preCmp IsNot Nothing AndAlso cmd._oldMark = cmd._mark Then
    '                Return cmd._preCmp
    '            Else
    '                Throw New InvalidOperationException("Cannot convert to list")
    '            End If
    '        End Operator

    '#Region " Ctors "
    '        Public Sub New()
    '        End Sub

    '        'Public Sub New(ByVal table As SourceFragment)
    '        '    MyBase.New(table)
    '        'End Sub

    '        'Public Sub New(ByVal selectType As Type)
    '        '    MyBase.New(selectType)
    '        'End Sub

    '        'Public Sub New(ByVal entityName As String)
    '        '    MyBase.New(entityName)
    '        'End Sub

    '        'Public Sub New(ByVal obj As IKeyEntity)
    '        '    MyBase.New(obj)
    '        'End Sub

    '        'Public Sub New(ByVal obj As IKeyEntity, ByVal key As String)
    '        '    MyBase.New(obj, key)
    '        'End Sub

    '        Public Sub New(ByVal getMgr As ICreateManager)
    '            MyBase.New(getMgr)
    '        End Sub

    '        'Public Sub New(ByVal selectType As Type, ByVal getMgr As ICreateManager)
    '        '    MyBase.New(selectType, getMgr)
    '        'End Sub

    '        'Public Sub New(ByVal entityName As String, ByVal getMgr As ICreateManager)
    '        '    MyBase.New(entityName, getMgr)
    '        'End Sub

    '        'Public Sub New(ByVal obj As _IKeyEntity, ByVal getMgr As ICreateManager)
    '        '    MyBase.New(obj, getMgr)
    '        'End Sub

    '        'Public Sub New(ByVal obj As _IKeyEntity, ByVal key As String, ByVal getMgr As ICreateManager)
    '        '    MyBase.New(obj, key, getMgr)
    '        'End Sub

    '#End Region

    '        Public Function GetEnumerator() As System.Collections.Generic.IEnumerator(Of T) Implements System.Collections.Generic.IEnumerable(Of T).GetEnumerator
    '            Return CType(Me, ReadOnlyList(Of T)).GetEnumerator
    '        End Function

    '        Protected Function _GetEnumerator() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
    '            Return GetEnumerator()
    '        End Function

    '        'Public Overrides Property SelectedType() As System.Type
    '        '    Get
    '        '        If MyBase.SelectedType Is Nothing Then
    '        '            Return GetType(T)
    '        '        Else
    '        '            Return MyBase.SelectedType
    '        '        End If
    '        '    End Get
    '        '    Set(ByVal value As System.Type)
    '        '        MyBase.SelectedType = value
    '        '    End Set
    '        'End Property
    '    End Class

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