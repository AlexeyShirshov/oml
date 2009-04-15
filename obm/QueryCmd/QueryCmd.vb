Imports System.Collections.Generic
Imports Worm.Entities.Meta
Imports Worm.Criteria.Core
Imports Worm.Sorting
Imports Worm.Entities
Imports Worm.Criteria.Joins
Imports System.Reflection
Imports Worm.Criteria.Conditions
Imports Worm.Criteria.Values
Imports Worm.Misc
Imports System.Collections.ObjectModel
Imports Worm.Cache

Namespace Query

    '<Serializable()> _
    Public Class QueryCmd
        Implements ICloneable, Cache.IQueryDependentTypes, Criteria.Values.IQueryElement, IExecutionContext

#Region " Classes "

        <Serializable()> _
        Class SelectClauseDef
            Private _fields As ObjectModel.ReadOnlyCollection(Of SelectExpression)
            Private _types As ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))

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
        End Class

        <Serializable()> _
        Class FromClauseDef
            Public ObjectSource As EntityUnion
            Public Table As SourceFragment
            Public Query As QueryCmd
            Private _qeu As EntityUnion

            Public Sub New(ByVal table As SourceFragment)
                Me.Table = table
            End Sub

            Public Sub New(ByVal query As QueryCmd)
                Me.Query = query
            End Sub

            Public Sub New(ByVal [alias] As EntityAlias)
                Me.ObjectSource = New EntityUnion([alias])
            End Sub

            Public Sub New(ByVal t As Type)
                Me.ObjectSource = New EntityUnion(t)
            End Sub

            Public Sub New(ByVal entityName As String)
                Me.ObjectSource = New EntityUnion(entityName)
            End Sub

            Public Sub New(ByVal os As EntityUnion)
                Me.ObjectSource = os
            End Sub

            Public ReadOnly Property QueryEU() As EntityUnion
                Get
                    If Query IsNot Nothing Then
                        If _qeu Is Nothing Then
                            _qeu = New EntityUnion(New EntityAlias(Query))
                        End If
                        Return _qeu
                    End If
                    If ObjectSource IsNot Nothing AndAlso ObjectSource.AnyType Is Nothing AndAlso String.IsNullOrEmpty(ObjectSource.AnyEntityName) Then
                        Return ObjectSource
                    End If
                    Return Nothing
                End Get
            End Property

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
            Private _oldct As EntityUnion
            Protected _types As ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))
            Private _cmd As QueryCmd
            Private _f As FromClauseDef
            Private _sssl As ObjectModel.ReadOnlyCollection(Of SelectExpression)

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
                _oldct = cmd._createType
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
                    _cmd._createType = _oldct
                    If _types Is Nothing AndAlso _sssl Is Nothing Then
                        _cmd._sel = Nothing
                    ElseIf _types IsNot Nothing Then
                        _cmd._sel = New SelectClauseDef(_types)
                    ElseIf _sssl IsNot Nothing Then
                        _cmd._sel = New SelectClauseDef(_sssl)
                    Else
                        Throw New NotSupportedException
                    End If
                    _cmd._from = _f
                End If
            End Sub
        End Class

        Public Class GetDynamicKey4FilterEventArgs
            Inherits EventArgs

            Private _f As IGetFilter
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
#End Region

        Public Delegate Function GetDictionaryDelegate(ByVal key As String) As IDictionary

        Public Event CacheDictionaryRequired(ByVal sender As QueryCmd, ByVal args As CacheDictionaryRequiredEventArgs)
        Public Event ExternalDictionary(ByVal sender As QueryCmd, ByVal args As ExternalDictionaryEventArgs)
        Public Event GetDynamicKey4Filter(ByVal sender As QueryCmd, ByVal args As GetDynamicKey4FilterEventArgs)
        Public Event QueryPrepared(ByVal sender As QueryCmd, ByVal args As QueryPreparedEventArgs)

        Friend _sel As SelectClauseDef
        Protected _filter As IGetFilter
        Protected _group As ObjectModel.ReadOnlyCollection(Of Grouping)
        Protected _order As Sort
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
        Protected _autoJoins As Boolean
        Friend _from As FromClauseDef
        Protected _hint As String
        Protected _mark As Guid = Guid.NewGuid 'Environment.TickCount
        Protected _statementMark As Guid = Guid.NewGuid 'Environment.TickCount
        'Protected _returnType As Type
        'Protected _realType As Type
        'Private _m2mObject As IKeyEntity
        'Protected _m2mKey As String
        Protected _rn As TableFilter
        Friend _outer As QueryCmd
        Private _er As OrmManager.ExecutionResult
        'Private _selectSrc As ObjectSource
        Friend _resDic As Boolean
        Private _appendMain As Boolean?
        '<NonSerialized()> _
        Protected _getMgr As ICreateManager
        Private _name As String
        Private _execCnt As Integer
        '<NonSerialized()> _
        Private _schema As ObjectMappingEngine
        Friend _cacheSort As Boolean
        Private _autoFields As Boolean = True
        Private _timeout As Nullable(Of Integer)
        Private _poco As IDictionary
        Friend _createType As EntityUnion
        Private _unions As ReadOnlyCollection(Of Pair(Of Boolean, QueryCmd))
        Private _having As IGetFilter
        Friend _optimizeIn As IFilter
        Private _newMaps As IDictionary

#Region " Cache "
        '<NonSerialized()> _
        Friend _types As Dictionary(Of EntityUnion, IEntitySchema)
        '<NonSerialized()> _
        Friend _pdic As Dictionary(Of Type, IDictionary)
        '<NonSerialized()> _
        Friend _sl As List(Of SelectExpression)
        '<NonSerialized()> _
        Friend _f As IFilter
        '<NonSerialized()> _
        Friend _js As List(Of QueryJoin)
        Friend _ftypes As Dictionary(Of EntityUnion, Object)
        Friend _stypes As Dictionary(Of EntityUnion, Object)
#End Region

        'Friend Shared ReadOnly InnerTbl As New SourceFragment

        Public Sub OptimizeInFilter(ByVal inFilter As IFilter)
            _optimizeIn = inFilter
        End Sub

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
                Return New Pair(Of List(Of Object), FieldReference)(CType(CType(tmf.Value, InValue).Value, List(Of Object)), fr)
            End Get
        End Property

        Public ReadOnly Property CreateType() As EntityUnion
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

        Public Property CommandTimeout() As Nullable(Of Integer)
            Get
                Return _timeout
            End Get
            Set(ByVal value As Nullable(Of Integer))
                _timeout = value
            End Set
        End Property

        Public Property MappingEngine() As ObjectMappingEngine
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

        Public Property LastExecutionResult() As OrmManager.ExecutionResult
            Get
                Return _er
            End Get
            Protected Friend Set(ByVal value As OrmManager.ExecutionResult)
                _er = value
            End Set
        End Property

        Protected ReadOnly Property GetMappingEngine() As ObjectMappingEngine
            Get
                Using mgr As OrmManager = CreateManager.CreateManager
                    Return mgr.MappingEngine
                End Using
            End Get
        End Property

        Public ReadOnly Property CreateManager() As ICreateManager
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

        '<NonSerialized()> _
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
            _getMgr = New CreateManager(getMgr)
        End Sub

        Public Sub New(ByVal getMgr As ICreateManager)
            _getMgr = getMgr
        End Sub

        Public Sub New(ByVal mpe As ObjectMappingEngine, ByVal connectionString As String)
            _getMgr = New CreateManager(Function() New Worm.Database.OrmReadOnlyDBManager(mpe, connectionString))
        End Sub

        Public Sub New(ByVal mpe As ObjectMappingEngine, ByVal cache As CacheBase, ByVal connectionString As String)
            _getMgr = New CreateManager(Function() New Worm.Database.OrmReadOnlyDBManager(cache, mpe, New Worm.Database.SQLGenerator, connectionString))
        End Sub

        Public Sub New(ByVal cache As CacheBase, ByVal connectionString As String)
            _getMgr = New CreateManager(Function() New Worm.Database.OrmReadOnlyDBManager(cache, Worm.Database.OrmReadOnlyDBManager.DefaultMappingEngine, New Worm.Database.SQLGenerator, connectionString))
        End Sub

        Public Sub New(ByVal connectionString As String)
            MyClass.New(New ReadonlyCache, connectionString)
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

                If cmd.SelectTypes IsNot Nothing Then
                    Dim t As New List(Of Pair(Of EntityUnion, Boolean?))
                    For Each tp As Pair(Of EntityUnion, Boolean?) In _types
                        t.Add(New Pair(Of EntityUnion, Boolean?)(tp.First, False))
                    Next
                    cmd._sel = New SelectClauseDef(t)
                End If
            End Sub

            Public Sub cl_paging(ByVal sender As IExecutor, ByVal args As IExecutor.GetCacheItemEventArgs)
                args.Created = False
                SetCT2Nothing()
                RemoveHandler sender.OnGetCacheItem, AddressOf Me.cl_paging
            End Sub
        End Class

        Public Shared Sub Prepare(ByVal root As QueryCmd, ByVal executor As IExecutor, _
            ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, _
            ByVal stmt As StmtGenerator)

            Dim isanonym As Boolean = root._createType IsNot Nothing AndAlso GetType(AnonymousEntity).IsAssignableFrom(root._createType.GetRealType(schema))

            'Dim fs As New List(Of IFilter)
            For Each q As QueryCmd In New StmtQueryIterator(root)
                'Dim j As New List(Of Worm.Criteria.Joins.QueryJoin)
                'Dim c As List(Of SelectExpression) = Nothing
                q.Prepare(executor, schema, filterInfo, stmt, isanonym)
                'If f IsNot Nothing Then
                '    fs.Add(f)
                'End If

                'js.Add(j)
                'cs.Add(c)
            Next

            'Return fs.ToArray
        End Sub

        Private Sub CopySE(ByVal se As SelectExpression)
            _sl.Add(se)
            If (_outer IsNot Nothing OrElse _rn IsNot Nothing) AndAlso String.IsNullOrEmpty(se.ColumnAlias) Then
                se.ColumnAlias = "[" & se.GetIntoPropertyAlias & "]"
            End If
            'If se.ObjectSource Is Nothing AndAlso _from.AnyQuery IsNot Nothing Then
            '    se.ObjectSource = _from.QueryEU
            'End If
        End Sub

        Protected Sub PrepareSelectList(ByVal executor As IExecutor, ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean, ByVal schema As ObjectMappingEngine, _
                                        ByRef f As IFilter, ByVal filterInfo As Object)
            If isAnonym Then
                For Each se As SelectExpression In SelectList
                    CopySE(se)
                    If _poco IsNot Nothing Then
                        Dim t As Type = Nothing
                        If se.Into Is Nothing Then
                            For Each tp As Type In _poco.Keys
                                t = tp
                                Exit For
                            Next
                        Else
                            t = se.Into.GetRealType(schema)
                        End If
                        se.IntoPropertyAlias = t.Name & "-" & se.GetIntoPropertyAlias
                    End If
                Next
                For Each se As SelectExpression In SelectList
                    se.Prepare(executor, schema, filterInfo, stmt, isAnonym)
                    If _from IsNot Nothing Then Exit For
                    CheckFrom(se)
                Next
            Else
                If IsFTS AndAlso GetSelectedTypes(schema).Count > 1 Then
                    _appendMain = True
                End If

                For Each se As SelectExpression In SelectList
                    se.Prepare(executor, schema, filterInfo, stmt, isAnonym)
                    Dim os As EntityUnion = If(se.Into IsNot Nothing, se.Into, se.ObjectSource)
                    If os IsNot Nothing Then
                        If Not _types.ContainsKey(os) Then
                            Dim t As Type = os.GetRealType(schema)
                            _types.Add(os, schema.GetEntitySchema(t))
                        End If
                    End If
                    CheckFrom(se)
                Next
                If _types.Count > 0 Then

                    For Each de As KeyValuePair(Of EntityUnion, IEntitySchema) In _types
                        Dim t As Type = de.Key.GetRealType(schema)
                        Dim oschema As IEntitySchema = de.Value
                        If Not _pdic.ContainsKey(t) Then
                            Dim dic As IDictionary = schema.GetProperties(t, oschema)
                            _pdic.Add(t, dic)

                            Dim col As ICollection(Of SelectExpression) = GetSelectList(de.Key)
                            If col.Count > 0 Then
                                If AutoFields AndAlso _outer Is Nothing AndAlso (_types.Count > 1 OrElse _createType Is Nothing OrElse t Is _createType.GetRealType(schema)) Then
                                    For Each dice As DictionaryEntry In dic
                                        Dim pk As EntityPropertyAttribute = CType(dice.Key, EntityPropertyAttribute)
                                        If (pk.Behavior And Field2DbRelations.PK) = Field2DbRelations.PK Then
                                            Dim find As Boolean
                                            For Each fld As SelectExpression In col
                                                If fld.PropertyAlias = pk.PropertyAlias Then
                                                    find = True
                                                    Exit For
                                                End If
                                            Next
                                            If Not find Then
                                                Dim se As New SelectExpression(de.Key, pk.PropertyAlias)
                                                se.Attributes = pk.Behavior
                                                If Not _sl.Contains(se) Then
                                                    CopySE(se)
                                                End If
                                            End If
                                        End If
                                    Next
                                End If
                                Dim df As IDefferedLoading = TryCast(oschema, IDefferedLoading)
                                If df IsNot Nothing Then
                                    Dim sss()() As String = df.GetDefferedLoadPropertiesGroups
                                    If sss IsNot Nothing Then
                                        For Each se As SelectExpression In col
                                            Dim found As Boolean = False
                                            For Each ss() As String In sss
                                                For Each pr As String In ss
                                                    If se.PropertyAlias = pr Then
                                                        found = True
                                                        GoTo l2
                                                    End If
                                                Next
                                            Next
l2:
                                            If Not found Then
                                                CopySE(se)
                                            End If
                                        Next
                                    Else
                                        GoTo l1
                                    End If
                                Else
l1:
                                    For Each se As SelectExpression In col
                                        CopySE(se)
                                    Next
                                End If
                            End If
                        End If
                    Next

                    If _sl.Count = 0 Then
                        For Each se As SelectExpression In SelectList
                            CopySE(se)
                        Next
                    Else
                        For Each se As SelectExpression In SelectList
                            If se.IsCustom OrElse se.Aggregate IsNot Nothing OrElse se.PropType = PropType.TableColumn Then
                                CopySE(se)
                            End If
                        Next
                    End If

                    If AutoJoins Then
                        Dim selOS As EntityUnion = GetSelectedOS()
                        Dim t As Type = selOS.GetRealType(schema)
                        Dim selSchema As IEntitySchema = schema.GetEntitySchema(t)
                        For Each se As SelectExpression In SelectList
                            If se.ObjectSource IsNot Nothing Then
                                If Not HasInQuery(se.ObjectSource, _js) Then
                                    schema.AppendJoin(selOS, t, selSchema, _
                                        se.ObjectSource, se.ObjectSource.GetRealType(schema), schema.GetEntitySchema(se.ObjectSource.GetRealType(schema)), _
                                        f, _js, filterInfo)
                                End If
                            ElseIf se.Table IsNot Nothing Then
                                Throw New NotImplementedException
                            Else
                                Throw New NotImplementedException
                            End If
                        Next
                    End If
                Else
                    For Each se As SelectExpression In SelectList
                        CopySE(se)
                    Next
                End If
            End If
        End Sub

        Protected Overridable Sub _Prepare(ByVal executor As IExecutor, _
            ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, _
            ByVal stmt As StmtGenerator, ByRef f As IFilter, ByVal selectOS As EntityUnion, _
            ByVal isAnonym As Boolean)

            If _from IsNot Nothing Then
                Dim anq As QueryCmd = _from.AnyQuery
                If anq IsNot Nothing Then
                    Try
                        anq._outer = Me
                        Prepare(anq, executor, schema, filterInfo, stmt)
                    Finally
                        anq._outer = Nothing
                    End Try
                End If
            End If

            If SelectList IsNot Nothing Then
                PrepareSelectList(executor, stmt, isAnonym, schema, f, filterInfo)
            Else
                If IsFTS Then
                    For Each tp As Pair(Of EntityUnion, Boolean?) In SelectTypes
                        Dim os As EntityUnion = tp.First
                        Dim rt As Type = tp.First.GetRealType(schema)
                        Dim oschema As IEntitySchema = schema.GetEntitySchema(rt)
                        If _WithLoad(tp, schema) Then
                            _appendMain = True
                            _sl.AddRange(schema.GetSortedFieldList(rt, oschema).ConvertAll(Function(c As EntityPropertyAttribute) ObjectMappingEngine.ConvertColumn2SelExp(c, os)))
                        Else
                            Dim ctx As IContextObjectSchema = TryCast(oschema, IContextObjectSchema)
                            If ctx IsNot Nothing Then
                                Dim cf As IFilter = ctx.GetContextFilter(filterInfo)
                                If cf IsNot Nothing Then
                                    _appendMain = True
                                    _sl.AddRange(schema.GetPrimaryKeys(rt, oschema).ConvertAll(Function(c As EntityPropertyAttribute) ObjectMappingEngine.ConvertColumn2SelExp(c, os)))
                                    Continue For
                                End If
                            End If

                            Dim jb As IJoinBehavior = TryCast(oschema, IJoinBehavior)
                            If jb IsNot Nothing AndAlso jb.AlwaysJoinMainTable Then
                                _appendMain = True
                                _sl.AddRange(schema.GetPrimaryKeys(rt, oschema).ConvertAll(Function(c As EntityPropertyAttribute) ObjectMappingEngine.ConvertColumn2SelExp(c, os)))
                                Continue For
                            End If

                            Dim pk As String = schema.GetPrimaryKeys(rt, oschema)(0).PropertyAlias
                            Dim se As New SelectExpression(_from.Table, stmt.FTSKey, pk)
                            se.Into = os
                            se.Attributes = Field2DbRelations.PK
                            _sl.Add(se)
                        End If
                    Next
                Else
                    If SelectTypes IsNot Nothing Then
l1:
                        If _from IsNot Nothing AndAlso _from.Query IsNot Nothing Then
                            If SelectTypes.Count > 1 Then
                                Throw New NotSupportedException
                            Else
                                AddTypeFields(schema, _sl, SelectTypes(0), _from.QueryEU)
                            End If
                        Else
                            Dim selTypes As ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?)) = SelectTypes
                            If selTypes Is Nothing Then
                                If _from IsNot Nothing Then
                                    selTypes = New ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))(New Pair(Of EntityUnion, Boolean?)() {New Pair(Of EntityUnion, Boolean?)(_from.ObjectSource, Nothing)})
                                Else
                                    Throw New QueryCmdException("Neither SelectTypes nor FromClause not set", Me)
                                End If
                            End If
                            For Each tp As Pair(Of EntityUnion, Boolean?) In selTypes
                                AddTypeFields(schema, _sl, tp)
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
                                Dim t As Type = de.Key.GetRealType(schema)
                                If Not _pdic.ContainsKey(t) Then
                                    Dim dic As IDictionary = schema.GetProperties(t, de.Value)
                                    If Not GetType(IPropertyLazyLoad).IsAssignableFrom(t) Then
                                        Dim hasCmplx As Boolean = False
                                        For Each tde As DictionaryEntry In dic
                                            Dim pi As Reflection.PropertyInfo = CType(tde.Value, PropertyInfo)
                                            Dim ep As EntityPropertyAttribute = CType(tde.Key, EntityPropertyAttribute)
                                            Dim selex As SelectExpression = _sl.Find(Function(se As SelectExpression) se.PropertyAlias = ep.PropertyAlias)
                                            If selex IsNot Nothing Then
                                                Dim pit As Type = pi.PropertyType
                                                If ObjectMappingEngine.IsEntityType(pit, schema) _
                                                    AndAlso Not GetType(IPropertyLazyLoad).IsAssignableFrom(pit) Then
                                                    Dim eu As EntityUnion = selex.ObjectSource
                                                    If eu Is Nothing Then eu = New EntityUnion(pit)
                                                    If Not HasInQuery(eu, _js) Then
                                                        Dim s As IEntitySchema = Nothing
                                                        If Not GetType(IEntity).IsAssignableFrom(pit) Then
                                                            Dim hasPK As Boolean
                                                            s = GetSchema(schema, pit, hasPK)
                                                            AddPOCO(pit, s)
                                                        Else
                                                            s = schema.GetEntitySchema(pit, False)
                                                        End If
                                                        schema.AppendJoin(de.Key, t, de.Value, eu, pit, s, f, _js, filterInfo, ObjectMappingEngine.JoinFieldType.Direct, ep.PropertyAlias)
                                                        hasCmplx = True
                                                        SelectAdd(eu, True)
                                                    End If
                                                End If
                                            End If
                                        Next
                                        If hasCmplx Then
                                            _sl = New List(Of SelectExpression)
                                            _types = New Dictionary(Of EntityUnion, IEntitySchema)
                                            _pdic = New Dictionary(Of Type, IDictionary)
                                            GoTo l1
                                        End If
                                    End If
                                    _pdic.Add(t, dic)
                                End If
                            Next
                        End If
                    Else
                        'Dim s As IEntitySchema = GetSchemaForSelectType(schema)
                        'If _from IsNot Nothing AndAlso _from.Table IsNot Nothing AndAlso s IsNot Nothing Then
                        '    For Each m As MapField2Column In s.GetFieldColumnMap
                        '        Dim se As New SelectExpression(m.Table, m.Column)
                        '        se.Attributes = m._newattributes
                        '        se.PropertyAlias = m._propertyAlias
                        '        _sl.Add(se)
                        '    Next
                        '    'Else
                        '    '    Throw New NotSupportedException
                        'Else
                        If _from Is Nothing Then
                            Dim s As IEntitySchema = GetSchemaForSelectType(schema)
                            If s IsNot Nothing Then
                                _from = New FromClauseDef(s.Table)
                            End If
                        End If
                        If _poco IsNot Nothing Then
                            For Each de As DictionaryEntry In _poco
                                SelectAdd(New EntityUnion(CType(de.Key, Type)), Nothing)
                                GoTo l1
                            Next
                        End If
                        'If GetType(AnonymousEntity).IsAssignableFrom(_createType) Then
                        '    Throw New QueryCmdException("Neither SelectTypes nor SelectList specified", Me)
                        'End If
                    End If
                End If

                If AutoJoins Then
                    Dim t As Type = selectOS.GetRealType(schema)
                    Dim selSchema As IEntitySchema = _types(selectOS) 'schema.GetEntitySchema(t)
                    For Each tp As Pair(Of EntityUnion, Boolean?) In SelectTypes
                        If Not HasInQuery(tp.First, _js) Then
                            schema.AppendJoin(selectOS, t, selSchema, _
                                tp.First, tp.First.GetRealType(schema), schema.GetEntitySchema(tp.First.GetRealType(schema)), _
                                f, _js, filterInfo)
                        End If
                    Next
                End If
            End If

            _f = f
        End Sub

        Friend _prepared As Boolean
        Friend _cancel As Boolean

        Public Sub Prepare(ByVal executor As IExecutor, _
            ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, _
            ByVal stmt As StmtGenerator, ByVal isAnonym As Boolean) Implements IQueryElement.Prepare

            _sl = New List(Of SelectExpression)
            _types = New Dictionary(Of EntityUnion, IEntitySchema)
            _pdic = New Dictionary(Of Type, IDictionary)
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

            For Each s As Sort In New Sort.Iterator(_order)
                s.Prepare(executor, schema, filterInfo, stmt, isAnonym)
                If s.ObjectSource IsNot Nothing Then
                    'Dim tp As Type = s.ObjectSource.GetRealType(schema)
                    If Not _stypes.ContainsKey(s.ObjectSource) Then
                        _stypes.Add(s.ObjectSource, Nothing)
                    End If
                End If
            Next

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

                If AutoJoins Then
                    Dim joins() As Worm.Criteria.Joins.QueryJoin = Nothing
                    Dim appendMain As Boolean
                    If OrmManager.HasJoins(schema, selectType, f, Sort, filterInfo, joins, appendMain) Then
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
                If se.Aggregate IsNot Nothing Then
                    Dim a As Aggregate = TryCast(se.Aggregate, [Aggregate])
                    If a IsNot Nothing Then
                        Dim ep As SelectExpressionValue = TryCast(a.Expression.Value, SelectExpressionValue)
                        If ep IsNot Nothing Then
                            If ep.Expression.ObjectSource IsNot Nothing Then
                                _from = New FromClauseDef(ep.Expression.ObjectSource)
                            ElseIf ep.Expression.Table IsNot Nothing Then
                                _from = New FromClauseDef(ep.Expression.Table)
                            Else
                                Throw New NotSupportedException
                            End If
                        End If
                    End If
                ElseIf se.Table IsNot Nothing Then
                    _from = New FromClauseDef(se.Table)
                ElseIf se.ObjectSource IsNot Nothing Then
                    _from = New FromClauseDef(se.ObjectSource)
                End If
            End If
        End Sub

        Private Function HasInQuery(ByVal os As EntityUnion, ByVal js As List(Of QueryJoin)) As Boolean
            If FromClause IsNot Nothing AndAlso FromClause.ObjectSource.Equals(os) Then
                Return True
            Else
                For Each j As QueryJoin In js
                    If os.Equals(j.ObjectSource) OrElse os.Equals(j.M2MObjectSource) Then
                        Return True
                    End If
                Next
                If SelectTypes IsNot Nothing Then
                    For Each tp As Pair(Of EntityUnion, Boolean?) In SelectTypes
                        If os.Equals(tp.First) Then
                            Return True
                        End If
                    Next
                End If
            End If
            Return False
        End Function

        Protected Sub AddTypeFields(ByVal schema As ObjectMappingEngine, ByVal cl As List(Of SelectExpression), _
                                  ByVal tp As Pair(Of EntityUnion, Boolean?), Optional ByVal os As EntityUnion = Nothing)
            Dim t As Type = tp.First.GetRealType(schema)
            If os Is Nothing Then
                os = tp.First
            End If
            'Dim oschema As IEntitySchema = schema.GetEntitySchema(t, False)
            'If oschema Is Nothing Then
            '    oschema = GetEntitySchema(t)
            'End If
            Dim oschema As IEntitySchema = GetEntitySchema(schema, t)

            If oschema Is Nothing Then
                Throw New QueryCmdException(String.Format("Cannot find schema for type {0}", t), Me)
            End If

            If Not GetType(ICachedEntity).IsAssignableFrom(t) OrElse _WithLoad(tp, schema) Then
                Dim l As New List(Of SelectExpression)(schema.GetSortedFieldList(t, oschema).ConvertAll(Function(c As EntityPropertyAttribute) ObjectMappingEngine.ConvertColumn2SelExp(c, os)))
                Dim df As IDefferedLoading = TryCast(oschema, IDefferedLoading)
                If df IsNot Nothing Then
                    Dim sss()() As String = df.GetDefferedLoadPropertiesGroups
                    If sss IsNot Nothing Then
                        For Each ss() As String In sss
                            For Each pr As String In ss
                                Dim pr2 As String = pr
                                Dim idx As Integer = l.FindIndex(Function(pa As SelectExpression) pa.PropertyAlias = pr2)
                                If idx >= 0 Then
                                    l.RemoveAt(idx)
                                End If
                            Next
                        Next
                    End If
                End If
                Dim pod As Boolean = _poco IsNot Nothing AndAlso _poco.Contains(t)
                For Each se As SelectExpression In l
                    se.ObjectProperty = New ObjectProperty(tp.First, se.PropertyAlias)
                    If pod Then
                        se.IntoPropertyAlias = t.Name & "-" & se.PropertyAlias
                    End If
                    cl.Add(se)
                Next
            Else
                'If Need2MainType(os) Then
                For Each c As EntityPropertyAttribute In schema.GetPrimaryKeys(t, oschema)
                    Dim se As New SelectExpression(os, c.PropertyAlias)
                    se.Attributes = c.Behavior
                    se.Column = c.Column
                    cl.Add(se)
                Next
                '    Else

                'End If
            End If

            If Not _types.ContainsKey(os) Then
                _types.Add(os, oschema)
            End If
        End Sub

        Public Shared Function GetStaticKey(ByVal root As QueryCmd, ByVal mgrKey As String, ByVal cb As Cache.CacheListBehavior, _
            ByVal fromKey As String, ByVal mpe As ObjectMappingEngine, _
            ByRef dic As IDictionary, ByVal fi As Object) As String
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

                If Not q.GetStaticKey(key, cb_, mpe, fi) Then
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
                    q.GetStaticKey(key, Cache.CacheListBehavior.CacheAll, mpe, fi)
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

        Protected Friend Function GetStaticKey(ByVal sb As StringBuilder, _
            ByVal cb As Cache.CacheListBehavior, _
            ByVal mpe As ObjectMappingEngine, ByVal fi As Object) As Boolean

            If Not _prepared Then
                Throw New QueryCmdException("Command not prepared", Me)
            End If

            Dim sb2 As New StringBuilder

            Dim f As IFilter = _f
            Dim j As List(Of QueryJoin) = _js

            If f IsNot Nothing Then
                Select Case cb
                    Case Cache.CacheListBehavior.CacheAll
                        sb2.Append(f.GetStaticString(mpe, fi)).Append("$")
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
                            sb2.Append(f.GetStaticString(mpe, fi)).Append("$")
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
                        sb2.Append(join.GetStaticString(mpe, fi))
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
                    sb.Append(_from.ObjectSource.ToStaticString(mpe, fi))
                Else
                    sb.Append(_from.Query.ToStaticString(mpe, fi))
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
                sb.Append(_rn.ToStaticString(mpe, fi))
            End If

            If _top IsNot Nothing Then
                sb.Append(_top.GetStaticKey).Append("$")
            End If

            sb.Append(_distinct.ToString).Append("$")

            If SelectList IsNot Nothing Then
                Dim it As IList = SelectList
                If _types.Count < 2 Then
                    Dim l As New List(Of SelectExpression)(SelectList)
                    l.Sort(Function(s1 As SelectExpression, s2 As SelectExpression) s1._ToString.CompareTo(s2._ToString))
                    it = l
                End If
                For Each c As SelectExpression In it
                    If Not GetStaticKeyFromProp(sb, cb, c, mpe) Then
                        Return False
                    End If
                Next
                sb.Append("$")
            ElseIf SelectTypes IsNot Nothing Then
                For Each t As Pair(Of EntityUnion, Boolean?) In SelectTypes
                    sb.Append(t.First.ToStaticString(mpe, fi))
                Next
                sb.Append("$")
            End If

            If _group IsNot Nothing Then
                For Each g As Grouping In _group
                    If Not GetStaticKeyFromProp(sb, cb, g, mpe) Then
                        Return False
                    End If
                    sb.Append("$")
                Next
            End If

            If _having IsNot Nothing Then
                sb.Append(_having.Filter.GetStaticString(mpe, fi)).Append("$")
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

        Public Shared Function GetDynamicKey(ByVal root As QueryCmd) As String
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
                q.GetDynamicKey(id)
                i += 1
            Next

            Return id.ToString '& GetType(T).ToString
        End Function

        Protected Friend Sub GetDynamicKey(ByVal sb As StringBuilder)
            If Not _prepared Then
                Throw New QueryCmdException("Command not prepared", Me)
            End If

            Dim f As IFilter = _f
            Dim j As List(Of QueryJoin) = _js

            If f IsNot Nothing Then
                Dim args As New GetDynamicKey4FilterEventArgs(f)
                RaiseEvent GetDynamicKey4Filter(Me, args)
                If Not String.IsNullOrEmpty(args.CustomKey) Then
                    sb.Append(args.CustomKey).Append("$")
                Else
                    sb.Append(f._ToString).Append("$")
                End If
            End If

            If _having IsNot Nothing Then
                sb.Append(_having.Filter._ToString).Append("$")
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

            If _from IsNot Nothing Then
                If _from.ObjectSource IsNot Nothing Then
                    sb.Append(_from.ObjectSource._ToString())
                ElseIf _from.Query IsNot Nothing Then
                    sb.Append(_from.Query._ToString())
                End If
            Else
                Throw New NotSupportedException
            End If

            sb.Append("$")

            If _rn IsNot Nothing Then
                sb.Append(_rn.ToString)
            End If
        End Sub

        Public Overrides Function ToString() As String
            Throw New NotSupportedException
        End Function

        Public Function ToStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String
            Dim sb As New StringBuilder
            Dim l As List(Of SelectExpression) = Nothing
            GetStaticKey(sb, Cache.CacheListBehavior.CacheAll, mpe, contextFilter)
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
            If _from IsNot Nothing AndAlso _from.ObjectSource IsNot Nothing Then
                Return _from.ObjectSource
            Else
                If SelectTypes IsNot Nothing AndAlso SelectTypes.Count > 0 Then
                    Return SelectTypes(0).First
                    'ElseIf SelectList IsNot Nothing Then
                    '    Dim os As ObjectSource = SelectList(0).ObjectSource
                    '    For i As Integer = 1 To SelectList.Count - 1
                    '        If Not SelectList(i).ObjectSource.Equals(os) Then
                    '            Return Nothing
                    '        End If
                    '    Next
                    '    Return os
                End If
            End If
            Return Nothing
        End Function

        Public Function GetSelectedOS() As EntityUnion
            Dim os As EntityUnion = GetRealSelectedOS()
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

        Friend Function NeedSelectType(ByVal mpe As ObjectMappingEngine) As Boolean
            If SelectTypes IsNot Nothing Then
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

            If SelectTypes IsNot Nothing Then
                For Each p As Pair(Of EntityUnion, Boolean?) In SelectTypes
                    Dim os As EntityUnion = p.First
                    'If Not _dic.ContainsKey(os) Then
                    '    _dic.Add(os, Nothing)
                    'End If
                    ts.Add(os.GetRealType(mpe))
                Next
            ElseIf SelectList IsNot Nothing Then
                For Each s As SelectExpression In SelectList
                    If s.ObjectProperty.Entity Is Nothing Then
                        '_dic.Clear()
                        'ts.Clear()
                        'Exit For
                    Else
                        Dim t As Type = s.ObjectSource.GetRealType(mpe)
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
            If SelectTypes IsNot Nothing Then
                For Each tp As Pair(Of EntityUnion, Boolean?) In SelectTypes
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

        Private _oldStart As Integer
        Private _oldLength As Integer
        Private _oldRev As Boolean

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

        Public Property Group() As ObjectModel.ReadOnlyCollection(Of Grouping)
            Get
                Return _group
            End Get
            Set(ByVal value As ObjectModel.ReadOnlyCollection(Of Grouping))
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

        Public Overridable Property SelectTypes() As ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))
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

        Protected Function GetSelectList(ByVal os As EntityUnion) As ICollection(Of SelectExpression)
            Dim l As New List(Of SelectExpression)
            For Each se As SelectExpression In SelectList
                Dim os_ As EntityUnion = se.ObjectSource 'If(se.Into IsNot Nothing, se.Into, se.ObjectSource)
                If se.Into Is Nothing AndAlso os.Equals(os_) Then
                    l.Add(se)
                End If
            Next
            Return l
        End Function

        Protected Function GetSelectedTypes(ByVal mpe As ObjectMappingEngine) As IList(Of Type)
            Dim l As New List(Of Type)
            If SelectList IsNot Nothing Then
                For Each se As SelectExpression In SelectList
                    If se.ObjectSource IsNot Nothing Then
                        Dim t As Type = se.ObjectSource.GetRealType(mpe)
                        If Not l.Contains(t) Then
                            l.Add(t)
                        End If
                    End If
                Next
            End If
            Return l
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

        Public Property Sort() As Sort
            Get
                Return _order
            End Get
            Set(ByVal value As Sort)
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
                Return SelectTypes IsNot Nothing AndAlso SelectTypes(0).Second IsNot Nothing AndAlso SelectTypes(0).Second.Value
            End Get
        End Property

        Protected Friend ReadOnly Property propWithLoads() As Boolean()
            Get
                If SelectTypes Is Nothing Then
                    Dim r(_types.Count - 1) As Boolean
                    Return r
                Else
                    Dim r(SelectTypes.Count - 1) As Boolean
                    For i As Integer = 0 To SelectTypes.Count - 1
                        Dim t As Pair(Of EntityUnion, Boolean?) = SelectTypes(i)
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

        Public Function JoinAdd(ByVal joins() As QueryJoin) As QueryCmd
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

        Public Function Join(ByVal joins() As QueryJoin) As QueryCmd
            Me.Joins = joins
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

        Public Function OrderBy(ByVal value As Sort) As QueryCmd
            Sort = value
            Return Me
        End Function

        Public Function WhereAdd(ByVal filter As IGetFilter) As QueryCmd
            If Me.Filter Is Nothing Then
                Me.Filter = filter
            Else
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
            _createType = New EntityUnion(t)
            Return Me
        End Function

        Public Function Into(ByVal entityName As String) As QueryCmd
            _createType = New EntityUnion(entityName)
            Return Me
        End Function

#Region " From "

        Public Function From(ByVal t As Type) As QueryCmd
            _from = New FromClauseDef(t)
            RenewMark()
            Return Me
        End Function

        Public Function From(ByVal [alias] As EntityAlias) As QueryCmd
            _from = New FromClauseDef([alias])
            RenewMark()
            Return Me
        End Function

        Public Function From(ByVal entityName As String) As QueryCmd
            _from = New FromClauseDef(entityName)
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

        Public Function [Select](ByVal ParamArray t() As EntityUnion) As QueryCmd
            SelectTypes = New ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))( _
                Array.ConvertAll(Of EntityUnion, Pair(Of EntityUnion, Boolean?))(t, _
                    Function(item As EntityUnion) New Pair(Of EntityUnion, Boolean?)(item, Nothing)))
            Return Me
        End Function

        Public Function [Select](ByVal ParamArray t() As Type) As QueryCmd
            SelectTypes = New ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))( _
                Array.ConvertAll(Of Type, Pair(Of EntityUnion, Boolean?))(t, _
                    Function(item As Type) New Pair(Of EntityUnion, Boolean?)(New EntityUnion(item), Nothing)))
            Return Me
        End Function

        Public Function [Select](ByVal ParamArray entityNames() As String) As QueryCmd
            SelectTypes = New ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))( _
                Array.ConvertAll(Of String, Pair(Of EntityUnion, Boolean?))(entityNames, _
                    Function(item As String) New Pair(Of EntityUnion, Boolean?)(New EntityUnion(item), Nothing)))
            Return Me
        End Function

        Public Function [Select](ByVal ParamArray aliases() As EntityAlias) As QueryCmd
            SelectTypes = New ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))( _
                Array.ConvertAll(Of EntityAlias, Pair(Of EntityUnion, Boolean?))(aliases, _
                    Function(item As EntityAlias) New Pair(Of EntityUnion, Boolean?)(New EntityUnion(item), Nothing)))
            Return Me
        End Function

        Public Function [Select](ByVal fields() As SelectExpression) As QueryCmd
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
                            Return
                        End If
                    End If
                Next
            End If

            _sel = New SelectClauseDef(New List(Of Pair(Of EntityUnion, Boolean?))(New Pair(Of EntityUnion, Boolean?)() {New Pair(Of EntityUnion, Boolean?)(New EntityUnion(t), Nothing)}))
        End Sub

        Public Function [Select](ByVal eu As EntityUnion, ByVal withLoad As Boolean) As QueryCmd
            Dim l As New List(Of Pair(Of EntityUnion, Boolean?))(New Pair(Of EntityUnion, Boolean?)() {New Pair(Of EntityUnion, Boolean?)(eu, withLoad)})
            SelectTypes = New ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))(l)
            Return Me
        End Function

        Public Function [Select](ByVal t As Type, ByVal withLoad As Boolean) As QueryCmd
            Dim l As New List(Of Pair(Of EntityUnion, Boolean?))(New Pair(Of EntityUnion, Boolean?)() {New Pair(Of EntityUnion, Boolean?)(New EntityUnion(t), withLoad)})
            SelectTypes = New ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))(l)
            Return Me
        End Function

        Public Function [Select](ByVal entityName As String, ByVal withLoad As Boolean) As QueryCmd
            Dim l As New List(Of Pair(Of EntityUnion, Boolean?))(New Pair(Of EntityUnion, Boolean?)() {New Pair(Of EntityUnion, Boolean?)(New EntityUnion(entityName), withLoad)})
            SelectTypes = New ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))(l)
            Return Me
        End Function

        Public Function [Select](ByVal [alias] As EntityAlias, ByVal withLoad As Boolean) As QueryCmd
            Dim l As New List(Of Pair(Of EntityUnion, Boolean?))(New Pair(Of EntityUnion, Boolean?)() {New Pair(Of EntityUnion, Boolean?)(New EntityUnion([alias]), withLoad)})
            SelectTypes = New ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))(l)
            Return Me
        End Function

        Public Function [SelectAdd](ByVal eu As EntityUnion, ByVal withLoad As Boolean?) As QueryCmd
            Dim l As New List(Of Pair(Of EntityUnion, Boolean?))()
            If SelectTypes IsNot Nothing Then
                l.AddRange(SelectTypes)
            End If
            l.Add(New Pair(Of EntityUnion, Boolean?)(eu, withLoad))
            SelectTypes = New ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))(l)
            Return Me
        End Function

        Public Function [SelectAdd](ByVal t As Type, ByVal withLoad As Boolean) As QueryCmd
            Dim l As New List(Of Pair(Of EntityUnion, Boolean?))()
            If SelectTypes IsNot Nothing Then
                l.AddRange(SelectTypes)
            End If
            l.AddRange(New Pair(Of EntityUnion, Boolean?)() {New Pair(Of EntityUnion, Boolean?)(New EntityUnion(t), withLoad)})
            SelectTypes = New ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))(l)
            Return Me
        End Function

        Public Function [SelectAdd](ByVal entityName As String, ByVal withLoad As Boolean) As QueryCmd
            Dim l As New List(Of Pair(Of EntityUnion, Boolean?))()
            If SelectTypes IsNot Nothing Then
                l.AddRange(SelectTypes)
            End If
            l.AddRange(New Pair(Of EntityUnion, Boolean?)() {New Pair(Of EntityUnion, Boolean?)(New EntityUnion(entityName), withLoad)})
            SelectTypes = New ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))(l)
            Return Me
        End Function

        Public Function [SelectAdd](ByVal [alias] As EntityAlias, ByVal withLoad As Boolean) As QueryCmd
            Dim l As New List(Of Pair(Of EntityUnion, Boolean?))()
            If SelectTypes IsNot Nothing Then
                l.AddRange(SelectTypes)
            End If
            l.AddRange(New Pair(Of EntityUnion, Boolean?)() {New Pair(Of EntityUnion, Boolean?)(New EntityUnion([alias]), withLoad)})
            SelectTypes = New ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))(l)
            Return Me
        End Function

        Public Function [SelectAdd](ByVal fields() As SelectExpression) As QueryCmd
            Dim l As New List(Of SelectExpression)()
            If SelectList IsNot Nothing Then
                l.AddRange(SelectList)
            End If
            l.AddRange(fields)
            SelectList = New ObjectModel.ReadOnlyCollection(Of SelectExpression)(l)
            Return Me
        End Function

        Public Function GroupBy(ByVal fields() As Grouping) As QueryCmd
            Group = New ObjectModel.ReadOnlyCollection(Of Grouping)(fields)
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

#Region " ToLists "

        Public Function ToMatrix() As ReadonlyMatrix
            If _getMgr Is Nothing Then
                Throw New QueryCmdException("OrmManager required", Me)
            End If
            Return ToMatrix(_getMgr)
        End Function

        Public Function ToMatrix(ByVal getMgr As ICreateManager) As ReadonlyMatrix
            Using mgr As OrmManager = getMgr.CreateManager
                Using New SetManagerHelper(mgr, getMgr)
                    Return ToMatrix(mgr)
                End Using
            End Using
        End Function

        Public Function ToMatrix(ByVal mgr As OrmManager) As ReadonlyMatrix
            Return GetExecutor(mgr).Exec(mgr, Me)
        End Function

#Region " ToList "
        Public Function ToBaseEntity(Of T As _IEntity)(ByVal getMgr As ICreateManager, ByVal withLoad As Boolean) As IList(Of T)
            Using mgr As OrmManager = getMgr.CreateManager
                Using New SetManagerHelper(mgr, getMgr)
                    Return ToBaseEntity(Of T)(mgr, withLoad)
                End Using
            End Using
        End Function

        Public Function ToBaseEntity(Of T As _IEntity)(ByVal withLoad As Boolean) As IList(Of T)
            If _getMgr Is Nothing Then
                Throw New QueryCmdException("OrmManager required", Me)
            End If

            Return ToBaseEntity(Of T)(_getMgr, withLoad)
        End Function

        Public Function ToBaseEntity(Of T As _IEntity)(ByVal mgr As OrmManager) As IList(Of T)
            Return ToBaseEntity(Of T)(mgr, False)
        End Function

        Public Function ToBaseEntity(Of T As _IEntity)(ByVal mgr As OrmManager, ByVal withLoad As Boolean) As IList(Of T)
            If SelectList IsNot Nothing Then
                Throw New NotSupportedException("Multi types")
            Else
                Dim selOS As EntityUnion = GetSelectedOS()
                If selOS Is Nothing Then
                    selOS = New EntityUnion(GetType(T))
                ElseIf SelectTypes IsNot Nothing Then
                    If SelectTypes.Count > 1 OrElse SelectTypes(0).First IsNot selOS Then
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
                    Dim spk As String = mgr.MappingEngine.GetPrimaryKeys(st)(0).PropertyAlias
                    Dim types As ICollection(Of Type) = mgr.MappingEngine.GetDerivedTypes(st)
                    For Each tt As Type In types
                        Dim pk As String = mgr.MappingEngine.GetPrimaryKeys(tt)(0).PropertyAlias
                        JoinAdd(JCtor.left_join(tt).on(selOS, spk).eq(tt, pk))
                        SelectAdd(tt, withLoad)
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
            Dim st As Type = GetSelectedType(mgr.MappingEngine)
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
            Return CType(t.Invoke(Me, New Object() {mgr}), System.Collections.IList)
        End Function

        Public Function ToList(ByVal getMgr As ICreateManager) As IList
            Using mgr As OrmManager = getMgr.CreateManager
                Using New SetManagerHelper(mgr, getMgr)
                    Return ToList(mgr)
                End Using
            End Using
        End Function

        Public Function ToList() As IList
            If _getMgr Is Nothing Then
                Throw New QueryCmdException("OrmManager required", Me)
            End If

            Return ToList(_getMgr)
        End Function

        Public Function ToList(Of CreateType As {New, _ICachedEntity}, ReturnType As _ICachedEntity)(ByVal mgr As OrmManager) As ReadOnlyEntityList(Of ReturnType)
            Return GetExecutor(mgr).Exec(Of CreateType, ReturnType)(mgr, Me)
        End Function

        Public Function ToList(Of CreateType As {New, _ICachedEntity}, ReturnType As _ICachedEntity)(ByVal getMgr As ICreateManager) As ReadOnlyEntityList(Of ReturnType)
            Using mgr As OrmManager = getMgr.CreateManager
                Using New SetManagerHelper(mgr, getMgr)
                    Return ToList(Of CreateType, ReturnType)(mgr)
                End Using
            End Using
        End Function

        Public Function ToList(Of CreateType As {New, _ICachedEntity}, ReturnType As _ICachedEntity)() As ReadOnlyEntityList(Of ReturnType)
            If _getMgr Is Nothing Then
                Throw New QueryCmdException("OrmManager required", Me)
            End If

            Return ToList(Of CreateType, ReturnType)(_getMgr)
        End Function

        Public Function ToList(Of CreateReturnType As {New, _ICachedEntity})(ByVal mgr As OrmManager) As ReadOnlyEntityList(Of CreateReturnType)
            Return GetExecutor(mgr).Exec(Of CreateReturnType, CreateReturnType)(mgr, Me)
        End Function

        Public Function ToList(Of CreateReturnType As {New, _ICachedEntity})(ByVal getMgr As ICreateManager) As ReadOnlyEntityList(Of CreateReturnType)
            Using mgr As OrmManager = getMgr.CreateManager
                Using New SetManagerHelper(mgr, getMgr)
                    Return ToList(Of CreateReturnType)(mgr)
                End Using
            End Using
        End Function

        Public Function ToList(Of CreateReturnType As {New, _ICachedEntity})() As ReadOnlyEntityList(Of CreateReturnType)
            If _getMgr Is Nothing Then
                Throw New QueryCmdException("OrmManager required", Me)
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
                Using New SetManagerHelper(mgr, getMgr)
                    Return ToAnonymList(mgr)
                End Using
            End Using
        End Function

        Public Function ToAnonymList() As ReadOnlyObjectList(Of AnonymousEntity)
            If _getMgr Is Nothing Then
                Throw New QueryCmdException("OrmManager required", Me)
            End If

            Return ToAnonymList(_getMgr)
        End Function

#End Region

#Region " ToEntityList "

        Public Function ToEntityList(Of T As {_ICachedEntity})(ByVal getMgr As CreateManagerDelegate) As ReadOnlyEntityList(Of T)
            Dim mgr As OrmManager = getMgr()
            Try
                Using New SetManagerHelper(mgr, getMgr)
                    Return ToEntityList(Of T)(mgr)
                End Using
            Finally
                If mgr IsNot Nothing Then
                    mgr.Dispose()
                End If
            End Try
        End Function

        Public Function ToEntityList(Of T As {_ICachedEntity})(ByVal getMgr As ICreateManager) As ReadOnlyEntityList(Of T)
            Using mgr As OrmManager = getMgr.CreateManager
                Using New SetManagerHelper(mgr, getMgr)
                    Return ToEntityList(Of T)(mgr)
                End Using
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
                Throw New QueryCmdException("OrmManager required", Me)
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
                Using New SetManagerHelper(mgr, getMgr)
                    Return ToOrmListDyn(Of T)(mgr)
                End Using
            End Using
        End Function

        Public Function ToOrmListDyn(Of T As _IKeyEntity)() As ReadOnlyList(Of T)
            If _getMgr Is Nothing Then
                Throw New QueryCmdException("OrmManager required", Me)
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
                Using New SetManagerHelper(mgr, getMgr)
                    Return GetExecutor(mgr).ExecSimple(Of T)(mgr, Me)
                End Using
            End Using
        End Function

        Public Function ToSimpleList(Of T)() As IList(Of T)
            If _getMgr Is Nothing Then
                Throw New QueryCmdException("OrmManager required", Me)
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
        '        Throw New QueryCmdException("OrmManager required", me)
        '    End If

        '    Return ToSimpleList(Of CreateType, T)(_getMgr)
        'End Function
#End Region

        Public Function Count(ByVal mgr As OrmManager) As Integer
            Dim s As Sort = _order
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
                        If SelectTypes Is Nothing Then
                            Throw New QueryCmdException("Neither FromClause nor SelectTypes not specified", Me)
                        End If
                        From(SelectTypes(0).First)
                    End If
                    Return [Select](FCtor.count).SingleSimple(Of Integer)(mgr)
                End Using
            Finally
                Sort = s
                _clientPage = p
                _pager = pp
                _rn = rf
            End Try
        End Function

        Public Function Count(ByVal getMgr As ICreateManager) As Integer
            Using mgr As OrmManager = getMgr.CreateManager
                Using New SetManagerHelper(mgr, getMgr)
                    Return Count(mgr)
                End Using
            End Using
        End Function

        Public Function Count() As Integer
            If _getMgr Is Nothing Then
                Throw New QueryCmdException("OrmManager required", Me)
            End If

            Return Count(_getMgr)
        End Function

        Public Function ToDictionary(Of TKey As ICachedEntity, TValue As ICachedEntity)(ByVal mgr As OrmManager) As IDictionary(Of TKey, IList(Of TValue))
            Dim c As New QueryCmd.svct(Me)
            Using New OnExitScopeAction(AddressOf c.SetCT2Nothing)
                If SelectClause Is Nothing Then
                    [Select](GetType(TKey), GetType(TValue))
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
            End Using
        End Function

        Public Function ToDictionary(Of TKey As ICachedEntity, TValue As ICachedEntity)() As IDictionary(Of TKey, IList(Of TValue))
            If _getMgr Is Nothing Then
                Throw New QueryCmdException("OrmManager required", Me)
            End If

            Return ToDictionary(Of TKey, TValue)(_getMgr)
        End Function

        Public Function ToDictionary(Of TKey As ICachedEntity, TValue As ICachedEntity)(ByVal getMgr As ICreateManager) As IDictionary(Of TKey, IList(Of TValue))
            Using mgr As OrmManager = getMgr.CreateManager
                Using New SetManagerHelper(mgr, getMgr)
                    Return ToDictionary(Of TKey, TValue)(mgr)
                End Using
            End Using
        End Function

        Public Function ToSimpleDictionary(Of TKey, TValue)(ByVal mgr As OrmManager) As IDictionary(Of TKey, IList(Of TValue))
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
            If _getMgr Is Nothing Then
                Throw New QueryCmdException("OrmManager required", Me)
            End If

            Return ToSimpleDictionary(Of TKey, TValue)(_getMgr)
        End Function

        Public Function ToSimpleDictionary(Of TKey, TValue)(ByVal getMgr As ICreateManager) As IDictionary(Of TKey, IList(Of TValue))
            Using mgr As OrmManager = getMgr.CreateManager
                Using New SetManagerHelper(mgr, getMgr)
                    Return ToSimpleDictionary(Of TKey, TValue)(mgr)
                End Using
            End Using
        End Function

        Public Function ToObjectList(Of T As _IEntity)() As ReadOnlyObjectList(Of T)
            If _getMgr Is Nothing Then
                Throw New QueryCmdException("OrmManager required", Me)
            End If

            Return ToObjectList(Of T)(_getMgr)
        End Function

        Public Function ToObjectList(Of T As _IEntity)(ByVal getMgr As ICreateManager) As ReadOnlyObjectList(Of T)
            Using mgr As OrmManager = getMgr.CreateManager
                Using New SetManagerHelper(mgr, getMgr)
                    Return ToObjectList(Of T)(mgr)
                End Using
            End Using
        End Function

        Public Function ToObjectList(Of T As _IEntity)(ByVal mgr As OrmManager) As ReadOnlyObjectList(Of T)
            If GetType(AnonymousEntity).IsAssignableFrom(GetType(T)) AndAlso _createType Is Nothing Then
                Return GetExecutor(mgr).ExecEntity(Of AnonymousEntity, T)(mgr, Me)
            ElseIf GetType(CachedEntity).IsAssignableFrom(GetType(T)) Then
                Return CType(ToList(mgr), Global.Worm.ReadOnlyObjectList(Of T))
            Else
                Return GetExecutor(mgr).ExecEntity(Of T)(mgr, Me)
            End If
        End Function

        Public Function ToPOCOList(Of T As {New, Class})() As IList(Of T)
            If _getMgr Is Nothing Then
                Throw New QueryCmdException("OrmManager required", Me)
            End If

            Using mgr As OrmManager = _getMgr.CreateManager
                Using New SetManagerHelper(mgr, CreateManager)
                    Return ToPOCOList(Of T)(mgr)
                End Using
            End Using
        End Function

        Private Sub AddPOCO(ByVal rt As Type, ByVal selSchema As IEntitySchema)
            If _poco Is Nothing Then
                _poco = Hashtable.Synchronized(New Hashtable)
            End If

            If Not _poco.Contains(rt) Then
                _poco.Add(rt, selSchema)
            End If
        End Sub

        Public Function ToPOCOList(Of T As {New, Class})(ByVal mgr As OrmManager) As IList(Of T)
            Dim rt As Type = GetType(T)
            Dim mpe As ObjectMappingEngine = mgr.MappingEngine

            Dim hasPK As Boolean
            Dim selSchema As IEntitySchema = GetSchema(mpe, rt, hasPK)

            AddPOCO(rt, selSchema)

            Dim l As IEnumerable = Nothing
            Dim r As New List(Of T)

            'Using New ObjectMappingEngine.CustomTypes(mpe, GetCustomTypes)
            If hasPK Then
                l = ToObjectList(Of AnonymousCachedEntity)(mgr)
            Else
                l = ToObjectList(Of AnonymousEntity)(mgr)
            End If
            'End Using

            Dim props As IDictionary = mpe.GetProperties(rt, selSchema)
            For Each e As _IEntity In l
                Dim ctd As ComponentModel.ICustomTypeDescriptor = CType(e, ComponentModel.ICustomTypeDescriptor)
                Dim ro As New T
                InitPOCO(props, rt, ctd, mpe, e, ro, mgr)
                r.Add(ro)
            Next

            Return r
        End Function

        Private Sub InitPOCO(ByVal props As IDictionary, ByVal rt As Type, _
            ByVal ctd As ComponentModel.ICustomTypeDescriptor, ByVal mpe As ObjectMappingEngine, _
            ByVal e As _IEntity, ByVal ro As Object, ByVal mgr As OrmManager)
            For Each kv As DictionaryEntry In props
                Dim col As EntityPropertyAttribute = CType(kv.Key, EntityPropertyAttribute)
                Dim pa As String = rt.Name & "-" & col.PropertyAlias
                If ctd.GetProperties.Find(pa, False) IsNot Nothing Then
                    Dim pi As Reflection.PropertyInfo = CType(kv.Value, PropertyInfo)
                    Dim v As Object = mpe.GetPropertyValue(e, pa, Nothing)
                    If v Is DBNull.Value Then
                        v = Nothing
                    End If
                    'pi.SetValue(ro, v, Nothing)
                    Dim pit As Type = pi.PropertyType
                    v = ObjectMappingEngine.SetValue(pit, mpe, mgr.Cache, v, ro, pi, col.PropertyAlias, Nothing, mgr.GetContextInfo)
                    If v IsNot Nothing AndAlso _poco.Contains(pit) Then
                        InitPOCO(mpe.GetProperties(pit, CType(_poco(pit), IEntitySchema)), pit, ctd, mpe, e, v, mgr)
                    End If
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
            Try
                Dim l As ReadOnlyEntityList(Of T) = Top(1).ToList(Of T)(mgr)
                If l.Count = 0 Then
                    Throw New InvalidOperationException("Number of items is " & l.Count)
                End If
                Return l(0)
            Finally
                _top = oldT
            End Try
        End Function

        Public Function First(Of T As {New, _ICachedEntity})(ByVal getMgr As ICreateManager) As T
            Dim oldT As Top = TopParam
            Try
                Dim l As ReadOnlyEntityList(Of T) = Top(1).ToList(Of T)(getMgr)
                If l.Count = 0 Then
                    Throw New InvalidOperationException("Number of items is " & l.Count)
                End If
                Return l(0)
            Finally
                _top = oldT
            End Try
        End Function

        Public Function First(Of T As {New, _ICachedEntity})() As T
            Dim oldT As Top = TopParam
            Try
                Dim l As ReadOnlyEntityList(Of T) = Top(1).ToList(Of T)()
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

#End Region

        Private Function GetSchema(ByVal mpe As ObjectMappingEngine, ByVal t As Type, _
                                   ByRef pk As Boolean) As IEntitySchema
            Dim s As IEntitySchema = ObjectMappingEngine.InitType(t, mpe, Nothing, Nothing)
            If s IsNot Nothing AndAlso s.GetType IsNot GetType(SimpleObjectSchema) Then Return s
            If SelectList Is Nothing Then
                Dim tbl As SourceFragment = Nothing
                If _from IsNot Nothing Then
                    tbl = _from.Table
                End If
                If tbl Is Nothing Then
                    tbl = ObjectMappingEngine.GetTable(mpe, t)
                End If
                Dim selList As New OrmObjectIndex
                For Each de As DictionaryEntry In ObjectMappingEngine.GetMappedProperties(t)
                    Dim c As EntityPropertyAttribute = CType(de.Key, EntityPropertyAttribute)
                    selList.Add(New MapField2Column(c.PropertyAlias, c.Column, tbl))
                Next
                Return New SimpleObjectSchema(selList)
            Else
                Dim cols As Collections.IndexedCollection(Of String, MapField2Column) = SelectExpression.GetMapping(SelectList)
                Dim tbl As SourceFragment = Nothing, hasTable As Boolean = False
                For Each c As MapField2Column In cols
                    If c.Table Is Nothing Then
                        If Not hasTable Then
                            If _from IsNot Nothing Then
                                tbl = _from.Table
                            End If
                            If tbl Is Nothing Then
                                tbl = ObjectMappingEngine.GetTable(mpe, t)
                            End If
                            hasTable = True
                        End If
                        c.Table = tbl
                    End If
                Next
                Return New SimpleObjectSchema(cols)
            End If
        End Function

        Protected Friend Function GetSchemaForSelectType(ByVal mpe As ObjectMappingEngine) As IEntitySchema
            If _poco Is Nothing Then
                Dim t As Type = GetSelectedType(mpe)
                If _createType Is Nothing OrElse _createType.GetRealType(mpe).IsAssignableFrom(t) Then
                    If t Is Nothing Then
                        Throw New QueryCmdException("Neither Into clause not specified nor ToAnonymous used", Me)
                    End If
                    Return mpe.GetEntitySchema(t, False)
                Else
                    Return Nothing
                End If
            Else
                For Each de As DictionaryEntry In _poco
                    Return CType(de.Value, IEntitySchema)
                Next
                Throw New QueryCmdException("impossible", Me)
            End If
        End Function

        Public Sub ClearCache(ByVal mgr As OrmManager)
            GetExecutor(mgr).ClearCache(mgr, Me)
        End Sub

        Public Sub RenewCache(ByVal mgr As OrmManager, ByVal v As Boolean)
            GetExecutor(mgr).RenewCache(mgr, Me, v)
        End Sub

        Public ReadOnly Property IsInCache(ByVal mgr As OrmManager) As Boolean
            Get
                Return GetExecutor(mgr).IsInCache(mgr, Me)
            End Get
        End Property

        Public Sub ClearCache()
            If _getMgr Is Nothing Then
                Throw New QueryCmdException("OrmManager required", Me)
            End If
            Using mgr As OrmManager = CreateManager.CreateManager
                GetExecutor(mgr).ClearCache(mgr, Me)
            End Using
        End Sub

        Public Sub RenewCache(ByVal v As Boolean)
            If _getMgr Is Nothing Then
                Throw New QueryCmdException("OrmManager required", Me)
            End If

            Using mgr As OrmManager = CreateManager.CreateManager
                GetExecutor(mgr).RenewCache(mgr, Me, v)
            End Using
        End Sub

        Public ReadOnly Property IsInCache() As Boolean
            Get
                If _getMgr Is Nothing Then
                    Throw New QueryCmdException("OrmManager required", Me)
                End If

                Using mgr As OrmManager = CreateManager.CreateManager
                    Return GetExecutor(mgr).IsInCache(mgr, Me)
                End Using
            End Get
        End Property

        Public Sub ResetObjects()
            If _getMgr Is Nothing Then
                Throw New QueryCmdException("OrmManager required", Me)
            End If

            ResetObjects(_getMgr)
        End Sub

        Public Sub ResetObjects(ByVal getMgr As ICreateManager)
            Using mgr As OrmManager = getMgr.CreateManager
                ResetObjects(mgr)
            End Using
        End Sub

        Public Sub ResetObjects(ByVal mgr As OrmManager)
            GetExecutor(mgr).ResetObjects(mgr, Me)
        End Sub

        Public Overridable Sub CopyTo(ByVal o As QueryCmd)
            With o
                '._aggregates = _aggregates
                ._appendMain = _appendMain
                ._autoJoins = _autoJoins
                ._clientPage = _clientPage
                ._distinct = _distinct
                ._dontcache = _dontcache
                ._exec = _exec
                ._sel = _sel
                ._filter = _filter
                ._group = _group
                ._hint = _hint
                ._joins = _joins
                '._m2mKey = _m2mKey
                '._load = _load
                ._mark = ._mark
                '._m2mObject = _m2mObject
                ._order = _order
                '._page = _page
                ._statementMark = _statementMark
                '._selectSrc = _selectSrc
                ._from = _from
                ._top = _top
                ._rn = _rn
                ._having = _having
                ._er = _er
                ._unions = _unions
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
                ._poco = _poco
                ._autoFields = _autoFields
            End With
        End Sub

        Public Overridable Function Clone() As Object Implements System.ICloneable.Clone
            Dim q As New QueryCmd
            CopyTo(q)
            Return q
        End Function

        Friend Function FindColumn(ByVal mpe As ObjectMappingEngine, ByVal p As String) As String

            For Each se As SelectExpression In _sl
                'If se.PropType = PropType.ObjectProperty Then
                If se.IntoPropertyAlias = p Then
                    Return If(String.IsNullOrEmpty(se.ColumnAlias), se.Column, se.ColumnAlias)
                Else
                    If se.PropertyAlias = p Then
                        Dim t As Type = se.ObjectSource.GetRealType(mpe)
                        If t Is Nothing Then
                            Return _from.AnyQuery.FindColumn(mpe, p)
                        Else
                            Dim oschema As IEntitySchema = GetEntitySchema(mpe, t)
                            Dim map As MapField2Column = GetFieldColumnMap(oschema, t)(se.PropertyAlias)
                            Return map.Column 'mpe.GetColumnNameByPropertyAlias(oschema, se.PropertyAlias, False, se.ObjectSource)
                        End If
                    End If
                End If
                'Else
                '    If se.Column = p Then
                '        Return p
                '    End If
                'End If
            Next

            Return Nothing
        End Function

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
            Dim singleType As Boolean = GetSelectedTypes(mpe, types)

            If singleType AndAlso Not dp.IsEmpty Then
                dp.AddBoth(types)
            End If

            If _filter IsNot Nothing AndAlso TryCast(_filter, IEntityFilter) Is Nothing Then
                For Each f As IFilter In _filter.Filter.GetAllFilters
                    Dim fdp As Cache.IDependentTypes = Cache.QueryDependentTypes(mpe, f)
                    If Cache.IsCalculated(fdp) Then
                        If singleType Then
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
                        If singleType Then
                            dp.AddBoth(types)
                        End If
                        dp.Merge(fdp)
                        'Else
                        '    Return fdp
                    End If
                Next
            End If

            If SelectList IsNot Nothing Then
                For Each f As SelectExpression In SelectList
                    If f.PropType = PropType.Subquery Then
                        Dim fdp As Cache.IDependentTypes = Cache.QueryDependentTypes(mpe, f)
                        If Cache.IsCalculated(fdp) Then
                            dp.Merge(fdp)
                            'Else
                            '    Return fdp
                        End If
                    End If
                Next
            End If
            Return dp.Get
        End Function

        'Public Function GetOrmCommand(Of T As {New, _IKeyEntity})(ByVal mgr As OrmManager) As OrmQueryCmd(Of T)
        '    'Dim f As ICreateQueryCmd = TryCast(mgr, ICreateQueryCmd)
        '    Dim q As New OrmQueryCmd(Of T)()
        '    CopyTo(q)
        '    If _getMgr Is Nothing Then
        '        q.Exec(mgr)
        '    End If
        '    Return q
        'End Function

        'Public Function GetOrmCommand(Of T As {New, _IKeyEntity})() As OrmQueryCmd(Of T)
        '    Dim mgr As OrmManager = OrmManager.CurrentManager
        '    Return GetOrmCommand(Of T)(mgr)
        'End Function

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
            GetDynamicKey(sb)
            Return sb.ToString
        End Function

        Public Function GetStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String Implements Criteria.Values.IQueryElement.GetStaticString
            Return ToStaticString(mpe, contextFilter)
        End Function

        Public Function [GetByID](Of T As {New, IKeyEntity})(ByVal id As Object, ByVal ensureLoaded As Boolean) As T
            If _getMgr IsNot Nothing Then
                Using mgr As OrmManager = _getMgr.CreateManager
                    Return GetByID(Of T)(id, ensureLoaded, mgr)
                End Using
            Else
                Throw New QueryCmdException("Manager is required", Me)
            End If

        End Function

        Public Function [GetByID](Of T As {New, IKeyEntity})(ByVal id As Object) As T
            Return GetByID(Of T)(id, False)
        End Function

        Public Function [GetByID](Of T As {New, IKeyEntity})(ByVal id As Object, ByVal ensureLoaded As Boolean, ByVal mgr As OrmManager) As T
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

            Dim o As IKeyEntity = Nothing
            Using New SetManagerHelper(mgr, CreateManager)
                Dim oldSch As ObjectMappingEngine = mgr.MappingEngine
                If MappingEngine IsNot Nothing AndAlso Not oldSch.Equals(MappingEngine) Then
                    mgr.SetSchema(MappingEngine)
                End If
                Try
                    If GetType(T) IsNot tp Then
                        'o = mgr.LoadType(id, tp, ensureLoaded, False)
                        If ensureLoaded Then
                            o = mgr.GetKeyEntityFromCacheOrDB(id, tp)
                        Else
                            o = mgr.GetKeyEntityFromCacheOrCreate(id, tp)
                        End If
                    Else
                        'o = mgr.LoadType(Of T)(id, ensureLoaded, False)
                        If ensureLoaded Then
                            o = mgr.GetKeyEntityFromCacheOrDB(Of T)(id)
                        Else
                            o = mgr.GetKeyEntityFromCacheOrCreate(Of T)(id)
                        End If
                    End If
                Finally
                    mgr.SetSchema(oldSch)
                End Try
            End Using

            If o IsNot Nothing AndAlso o.CreateManager Is Nothing AndAlso _getMgr IsNot Nothing Then
                o.SetCreateManager(_getMgr)
            End If

            Return CType(o, T)
        End Function

        Protected Function BuildDic(Of T As {New, _IEntity})(ByVal mgr As OrmManager, _
            ByVal firstPropertyAlias As String, ByVal secondPropertyAlias As String, ByVal level As Integer) As DicIndexT(Of T)

            Dim last As DicIndexT(Of T) = DicIndexT(Of T).CreateRoot(firstPropertyAlias, secondPropertyAlias, Me)
            Dim root As DicIndexT(Of T) = last
            Dim first As Boolean = True

            Dim pn As String = SelectList(0).IntoPropertyAlias
            If String.IsNullOrEmpty(pn) Then
                pn = SelectList(0).PropertyAlias
            End If
            Dim cn As String = SelectList(1).IntoPropertyAlias
            If String.IsNullOrEmpty(cn) Then
                cn = SelectList(1).PropertyAlias
            End If

            For Each a As AnonymousEntity In ToAnonymList(mgr)
                OrmManager.BuildDic(Of DicIndexT(Of T), T)(CStr(a(pn)), CInt(a(cn)), level, root, last, first, firstPropertyAlias, secondPropertyAlias)
            Next

            Return root
        End Function

        Public Function BuildDictionary(Of T As {New, _IEntity})(ByVal level As Integer) As DicIndexT(Of T)
            If _getMgr Is Nothing Then
                Throw New QueryCmdException("OrmManager required", Me)
            End If

            Return BuildDictionary(Of T)(_getMgr, level)
        End Function

        Public Function BuildDictionary(Of T As {New, _IEntity})(ByVal getMgr As ICreateManager, ByVal level As Integer) As DicIndexT(Of T)
            Using mgr As OrmManager = getMgr.CreateManager
                Using New SetManagerHelper(mgr, getMgr)
                    Return BuildDictionary(Of T)(mgr, level)
                End Using
            End Using
        End Function

        Public Function BuildDictionary(Of T As {New, _IEntity})(ByVal propertyAlias As String, ByVal level As Integer) As DicIndexT(Of T)
            If _getMgr Is Nothing Then
                Throw New QueryCmdException("OrmManager required", Me)
            End If

            Return BuildDictionary(Of T)(_getMgr, propertyAlias, level)
        End Function

        Public Function BuildDictionary(Of T As {New, _IEntity})(ByVal propertyAlias As String, ByVal secondPropertyAlias As String, ByVal level As Integer) As DicIndexT(Of T)
            If _getMgr Is Nothing Then
                Throw New QueryCmdException("OrmManager required", Me)
            End If

            Return BuildDictionary(Of T)(_getMgr, propertyAlias, secondPropertyAlias, level)
        End Function

        Public Function BuildDictionary(Of T As {New, _IEntity})(ByVal getMgr As ICreateManager, ByVal propertyAlias As String, ByVal level As Integer) As DicIndexT(Of T)
            Using mgr As OrmManager = getMgr.CreateManager
                Using New SetManagerHelper(mgr, getMgr)
                    Return BuildDictionary(Of T)(mgr, propertyAlias, level)
                End Using
            End Using
        End Function

        Public Function BuildDictionary(Of T As {New, _IEntity})(ByVal getMgr As ICreateManager, _
            ByVal propertyAlias As String, ByVal secondPropertyAlias As String, ByVal level As Integer) As DicIndexT(Of T)
            Using mgr As OrmManager = getMgr.CreateManager
                Using New SetManagerHelper(mgr, getMgr)
                    Return BuildDictionary(Of T)(mgr, propertyAlias, secondPropertyAlias, level)
                End Using
            End Using
        End Function

        Public Function BuildDictionary(Of T As {New, _IEntity})(ByVal mgr As OrmManager, ByVal level As Integer) As DicIndexT(Of T)

            If _group Is Nothing OrElse _group.Count = 0 Then
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

            If _group(0).Values Is Nothing OrElse _group(0).Values.Length = 0 Then
                Throw New InvalidOperationException("Group is not custom")
            End If

            Dim n As String = Nothing
            Dim se As SelectExpressionValue = TryCast(_group(0).Values(0), SelectExpressionValue)
            If se Is Nothing Then
                'Dim ev As EntityValue = TryCast(_group(0).Values(0), EntityValue)
                'If ev Is Nothing Then
                Throw New InvalidOperationException("Group is not object property reference")
                'End If
                'n = ev
            Else
                If se.Expression.PropType <> PropType.ObjectProperty Then
                    Throw New InvalidOperationException("Group is not object property reference")
                End If
                n = se.Expression.ObjectProperty.PropertyAlias
            End If

            Return BuildDic(Of T)(mgr, n, Nothing, level)
        End Function

        Public Function BuildDictionary(Of T As {New, _IEntity})(ByVal mgr As OrmManager, ByVal propertyAlias As String, ByVal level As Integer) As DicIndexT(Of T)
            Dim tt As Type = GetType(T)
            Dim c As New QueryCmd.svct(Me)
            Using New OnExitScopeAction(AddressOf c.SetCT2Nothing)
                Dim g As ObjectModel.ReadOnlyCollection(Of Grouping) = Nothing
                If _group IsNot Nothing Then
                    g = New ObjectModel.ReadOnlyCollection(Of Grouping)(_group)
                End If
                Dim srt As Sort = Nothing
                If _order IsNot Nothing Then
                    srt = _order
                End If

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
                    s = FCtor.custom("Pref", String.Format(mgr.StmtGenerator.Left, "{0}", level), FCtor.prop(so, propertyAlias))
                Else
                    s = FCtor.custom("Pref", String.Format(mgr.StmtGenerator.Left, "{0}", level), FCtor.prop(tt, propertyAlias))
                End If

                Try
                    [Select](FCtor.Exp(s).count("Count")) _
                        .GroupBy(FCtor.Exp(s)) _
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

            If Not String.IsNullOrEmpty(secondPropertyAlias) Then
                Dim tt As Type = GetType(T)

                If _from IsNot Nothing AndAlso _from.ObjectSource IsNot Nothing Then
                    tt = _from.ObjectSource.GetRealType(mgr.MappingEngine)
                End If

                Dim s1 As SelectExpression = FCtor.custom("Pref", String.Format(mgr.StmtGenerator.Left, "{0}", level), FCtor.prop(tt, firstPropertyAlias))
                Dim s2 As SelectExpression = FCtor.custom("Pref", String.Format(mgr.StmtGenerator.Left, "{0}", level), FCtor.prop(tt, secondPropertyAlias))

                Dim c As New QueryCmd.svct(Me)
                Using New OnExitScopeAction(AddressOf c.SetCT2Nothing)

                    Dim g As ObjectModel.ReadOnlyCollection(Of Grouping) = Nothing
                    If _group IsNot Nothing Then
                        g = New ObjectModel.ReadOnlyCollection(Of Grouping)(_group)
                    End If
                    Dim srt As Sort = Nothing
                    If _order IsNot Nothing Then
                        srt = _order
                    End If

                    Try
                        From( _
                            New QueryCmd() _
                                .[Select](FCtor.Exp(s1).count("Count")) _
                                .From(tt) _
                                .GroupBy(FCtor.Exp(s1)) _
                                .UnionAll( _
                            New QueryCmd() _
                                .[Select](FCtor.Exp(s2).count("Count")) _
                                .From(tt) _
                                .GroupBy(FCtor.Exp(s2)) _
                            ) _
                        ) _
                        .Select(FCtor.prop("Pref").sum("Count", "Count")) _
                        .GroupBy(FCtor.prop("Pref")) _
                        .OrderBy(SCtor.prop("Pref").desc)

                        Return BuildDic(Of T)(mgr, firstPropertyAlias, secondPropertyAlias, level)
                    Finally
                        Group = g
                        _order = srt
                    End Try
                End Using
            Else
                Return BuildDictionary(Of T)(mgr, firstPropertyAlias, level)
            End If
        End Function

        Public Function ContainsDyn(Of T As _ICachedEntity)(ByVal o As T) As Boolean
            Dim tt As Type = Nothing
            If SelectTypes IsNot Nothing AndAlso SelectTypes.Count = 1 Then
                Dim eu As EntityUnion = SelectTypes(0).First
                tt = eu.GetRealType(GetMappingEngine)
                Dim oldf As IGetFilter = Filter
                Try
                    WhereAdd(Ctor.prop(eu, GetMappingEngine.GetPrimaryKeys(tt)(0).PropertyAlias).eq(o))
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
            If SelectTypes IsNot Nothing AndAlso SelectTypes.Count = 1 Then
                Dim eu As EntityUnion = SelectTypes(0).First
                tt = eu.GetRealType(GetMappingEngine)
                Dim oldf As IGetFilter = Filter
                Try
                    WhereAdd(Ctor.prop(eu, GetMappingEngine.GetPrimaryKeys(tt)(0).PropertyAlias).eq(o))
                    Return SingleOrDefault(Of T)() IsNot Nothing
                Finally
                    _filter = oldf
                End Try
            Else
                Throw New QueryCmdException("QueryCmd doesn't select entity", Me)
            End If
        End Function

        Public Function GetEntitySchema(ByVal mpe As ObjectMappingEngine, ByVal t As System.Type) As Entities.Meta.IEntitySchema Implements IExecutionContext.GetEntitySchema2
            Dim oschema As IEntitySchema = mpe.GetEntitySchema(t, False)

            If oschema Is Nothing AndAlso _poco IsNot Nothing Then
                oschema = CType(_poco(t), IEntitySchema)
            End If

            If oschema Is Nothing Then
                Throw New QueryCmdException(String.Format("Object schema for type {0} is not defined", t), Me)
            End If

            Return oschema
        End Function

        Public Sub ReplaceSchema(ByVal mpe As ObjectMappingEngine, ByVal t As System.Type, ByVal newMap As Entities.Meta.OrmObjectIndex) Implements IExecutionContext.ReplaceSchema
            If _newMaps Is Nothing Then
                _newMaps = Hashtable.Synchronized(New Hashtable)
            End If
            _newMaps(t) = newMap
        End Sub

        Public Function GetFieldColumnMap(ByVal oschema As Entities.Meta.IEntitySchema, ByVal t As System.Type) As Collections.IndexedCollection(Of String, MapField2Column) Implements IExecutionContext.GetFieldColumnMap
            If _newMaps IsNot Nothing AndAlso _newMaps.Contains(t) Then
                Return CType(_newMaps(t), Worm.Collections.IndexedCollection(Of String, Worm.Entities.Meta.MapField2Column))
            End If
            Return oschema.GetFieldColumnMap
        End Function
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