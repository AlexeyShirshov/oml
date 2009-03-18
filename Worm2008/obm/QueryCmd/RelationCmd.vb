Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Cache
Imports Worm.Criteria.Core
Imports Worm.Criteria.Conditions
Imports Worm.Criteria.Joins
Imports Worm.Criteria.Values
Imports System.Collections.Generic

Namespace Query
    Public Class RelationCmd
        Inherits QueryCmd

        Friend _rel As Relation
        Private _desc As RelationDesc
        Private _fo As Criteria.FilterOperation = Criteria.FilterOperation.Equal

#Region " Ctors "
        Protected Sub New()
        End Sub

        Public Sub New(ByVal rel As Relation)
            _rel = rel
            [From](rel.Relation.Rel)
        End Sub

        Public Sub New(ByVal desc As RelationDesc, ByVal getMgr As CreateManagerDelegate)
            MyBase.New(getMgr)
            _desc = desc
            [From](desc.Rel)
        End Sub

        Public Sub New(ByVal desc As RelationDesc)
            _desc = desc
            [From](desc.Rel)
        End Sub

        Public Sub New(ByVal rel As Relation, ByVal getMgr As CreateManagerDelegate)
            MyBase.New(getMgr)
            _rel = rel
            [From](rel.Relation.Rel)
        End Sub

        Public Sub New(ByVal obj As IKeyEntity, ByVal t As Type)
            'MyBase.New(obj)
            If _desc Is Nothing Then
                _rel = New Relation(obj, New RelationDesc(New EntityUnion(t), Nothing))
            Else
                _rel = New Relation(obj, _desc)
            End If
            [From](_rel.Relation.Rel)
        End Sub

        Public Sub New(ByVal obj As IKeyEntity, ByVal eu As EntityUnion)
            'MyBase.New(obj)
            If _desc Is Nothing Then
                _rel = New Relation(obj, New RelationDesc(eu, Nothing))
            Else
                _rel = New Relation(obj, _desc)
            End If
            [From](_rel.Relation.Rel)
        End Sub

        Public Sub New(ByVal obj As IKeyEntity, ByVal eu As EntityUnion, ByVal key As String)
            'MyBase.New(obj, key)
            If _desc Is Nothing Then
                _rel = New Relation(obj, New RelationDesc(eu, Nothing, key))
            Else
                _rel = New Relation(obj, _desc)
            End If
            [From](_rel.Relation.Rel)
        End Sub

        Public Sub New(ByVal desc As RelationDesc, ByVal obj As IKeyEntity)
            'MyBase.New(obj, desc.Key)
            _rel = New Relation(obj, desc)
            [From](desc.Rel)
        End Sub

        Public Sub New(ByVal rel As Relation, ByVal getMgr As ICreateManager)
            MyBase.New(getMgr)
            _rel = rel
            [From](rel.Relation.Rel)
        End Sub

        Public Sub New(ByVal desc As RelationDesc, ByVal getMgr As ICreateManager)
            MyBase.New(getMgr)
            _desc = desc
            [From](desc.Rel)
        End Sub

        Public Sub New(ByVal obj As _IKeyEntity, ByVal eu As EntityUnion, ByVal getMgr As CreateManagerDelegate)
            MyBase.New(getMgr)
            If _desc Is Nothing Then
                _rel = New Relation(obj, New RelationDesc(eu, Nothing, Nothing))
            Else
                _rel = New Relation(obj, _desc)
            End If
            [From](_rel.Relation.Rel)
        End Sub

        Public Sub New(ByVal obj As _IKeyEntity, ByVal eu As EntityUnion, ByVal getMgr As ICreateManager)
            MyBase.New(getMgr)
            If _desc Is Nothing Then
                _rel = New Relation(obj, New RelationDesc(eu, Nothing, Nothing))
            Else
                _rel = New Relation(obj, _desc)
            End If
            [From](_rel.Relation.Rel)
        End Sub

        Public Sub New(ByVal obj As _IKeyEntity, ByVal eu As EntityUnion, ByVal key As String, ByVal getMgr As ICreateManager)
            MyBase.New(getMgr)
            If _desc Is Nothing Then
                _rel = New Relation(obj, New RelationDesc(eu, Nothing, key))
            Else
                _rel = New Relation(obj, _desc)
            End If
            [From](_rel.Relation.Rel)
        End Sub

        Public Sub New(ByVal desc As RelationDesc, ByVal obj As _IKeyEntity, ByVal getMgr As ICreateManager)
            MyBase.New(getMgr)
            _rel = New Relation(obj, desc)
            [From](desc.Rel)
        End Sub
#End Region

#Region " Create "
        Public Overloads Shared Function Create(ByVal desc As RelationDesc) As RelationCmd
            Return Create(desc, OrmManager.CurrentManager)
        End Function

        Public Overloads Shared Function Create(ByVal rel As Relation) As RelationCmd
            Return Create(rel, OrmManager.CurrentManager)
        End Function

        Public Overloads Shared Function Create(ByVal obj As IKeyEntity, ByVal en As EntityUnion) As RelationCmd
            Return Create(obj, en, OrmManager.CurrentManager)
        End Function

        Public Overloads Shared Function Create(ByVal obj As IKeyEntity, ByVal en As EntityUnion, ByVal key As String) As RelationCmd
            Return Create(obj, en, key, OrmManager.CurrentManager)
        End Function

        Public Overloads Shared Function Create(ByVal name As String, ByVal obj As IKeyEntity, ByVal en As EntityUnion) As RelationCmd
            Return Create(name, obj, en, OrmManager.CurrentManager)
        End Function

        Public Overloads Shared Function Create(ByVal name As String, ByVal obj As IKeyEntity, ByVal en As EntityUnion, ByVal key As String) As RelationCmd
            Return Create(name, obj, en, key, OrmManager.CurrentManager)
        End Function

        Public Overloads Shared Function Create(ByVal desc As RelationDesc, ByVal mgr As OrmManager) As RelationCmd
            Dim f As ICreateQueryCmd = TryCast(mgr, ICreateQueryCmd)
            Dim q As RelationCmd = Nothing
            If f Is Nothing Then
                q = New RelationCmd(desc)
            Else
                q = f.Create(desc)
            End If
            Dim cm As ICreateManager = TryCast(mgr, ICreateManager)
            If cm IsNot Nothing Then
                q._getMgr = cm
            End If
            Return q
        End Function

        Public Overloads Shared Function Create(ByVal rel As Relation, ByVal mgr As OrmManager) As RelationCmd
            Dim f As ICreateQueryCmd = TryCast(mgr, ICreateQueryCmd)
            Dim q As RelationCmd = Nothing
            If f Is Nothing Then
                q = New RelationCmd(rel)
            Else
                q = f.Create(rel)
            End If
            Dim cm As ICreateManager = TryCast(mgr, ICreateManager)
            If cm IsNot Nothing Then
                q._getMgr = cm
            End If
            Return q
        End Function

        Public Overloads Shared Function Create(ByVal obj As IKeyEntity, ByVal en As EntityUnion, ByVal mgr As OrmManager) As RelationCmd
            Dim f As ICreateQueryCmd = TryCast(mgr, ICreateQueryCmd)
            Dim q As RelationCmd = Nothing
            If f Is Nothing Then
                q = New RelationCmd(obj, en)
            Else
                q = f.Create(obj, en)
            End If
            Dim cm As ICreateManager = TryCast(mgr, ICreateManager)
            If cm IsNot Nothing Then
                q._getMgr = cm
            End If
            Return q
        End Function

        Public Overloads Shared Function Create(ByVal obj As IKeyEntity, ByVal en As EntityUnion, ByVal key As String, ByVal mgr As OrmManager) As RelationCmd
            Dim f As ICreateQueryCmd = TryCast(mgr, ICreateQueryCmd)
            Dim q As RelationCmd = Nothing
            If f Is Nothing Then
                q = New RelationCmd(obj, en, key)
            Else
                q = f.Create(obj, en, key)
            End If
            Dim cm As ICreateManager = TryCast(mgr, ICreateManager)
            If cm IsNot Nothing Then
                q._getMgr = cm
            End If
            Return q
        End Function

        Public Overloads Shared Function Create(ByVal name As String, ByVal obj As IKeyEntity, ByVal en As EntityUnion, ByVal mgr As OrmManager) As RelationCmd
            Dim f As ICreateQueryCmd = TryCast(mgr, ICreateQueryCmd)
            Dim q As RelationCmd = Nothing
            If f Is Nothing Then
                q = New RelationCmd(obj, en)
                q.Name = name
            Else
                q = f.Create(name, obj, en)
            End If
            Dim cm As ICreateManager = TryCast(mgr, ICreateManager)
            If cm IsNot Nothing Then
                q._getMgr = cm
            End If
            Return q
        End Function

        Public Overloads Shared Function Create(ByVal name As String, ByVal obj As IKeyEntity, ByVal en As EntityUnion, ByVal key As String, ByVal mgr As OrmManager) As RelationCmd
            Dim f As ICreateQueryCmd = TryCast(mgr, ICreateQueryCmd)
            Dim q As RelationCmd = Nothing
            If f Is Nothing Then
                q = New RelationCmd(obj, en, key)
                q.Name = name
            Else
                q = f.Create(name, obj, en, key)
            End If
            Dim cm As ICreateManager = TryCast(mgr, ICreateManager)
            If cm IsNot Nothing Then
                q._getMgr = cm
            End If
            Return q
        End Function

        'Public Shared Function Create(ByVal obj As IKeyEntity, ByVal mgr As OrmManager) As QueryCmd
        '    Dim f As ICreateQueryCmd = TryCast(mgr, ICreateQueryCmd)
        '    Dim q As QueryCmd = Nothing
        '    If f Is Nothing Then
        '        q = New QueryCmd(obj)
        '    Else
        '        q = f.Create(obj)
        '    End If
        '    Dim cm As ICreateManager = TryCast(mgr, ICreateManager)
        '    If cm IsNot Nothing Then
        '        q._getMgr = cm
        '    End If
        '    Return q
        'End Function

        'Public Shared Function Create(ByVal obj As IKeyEntity, ByVal key As String, ByVal mgr As OrmManager) As QueryCmd
        '    Dim f As ICreateQueryCmd = TryCast(mgr, ICreateQueryCmd)
        '    Dim q As QueryCmd = Nothing
        '    If f Is Nothing Then
        '        q = New QueryCmd(obj, key)
        '    Else
        '        q = f.Create(obj, key)
        '    End If
        '    Dim cm As ICreateManager = TryCast(mgr, ICreateManager)
        '    If cm IsNot Nothing Then
        '        q._getMgr = cm
        '    End If
        '    Return q
        'End Function

#End Region

#Region " Properties "
        Public Property FilterOperation() As Criteria.FilterOperation
            Get
                Return _fo
            End Get
            Set(ByVal value As Criteria.FilterOperation)
                _fo = value
            End Set
        End Property

        Public ReadOnly Property Relation() As Relation
            Get
                PrepareRel()
                Return _rel
            End Get
        End Property

        Public ReadOnly Property RelationDesc() As RelationDesc
            Get
                PrepareRel()
                Return _rel.Relation
            End Get
        End Property
#End Region

        Public Class ModifyFilter
            Inherits EventArgs

            Private _f As IFilter
            Public Property RelFilter() As IFilter
                Get
                    Return _f
                End Get
                Set(ByVal value As IFilter)
                    _f = value
                End Set
            End Property

            Private _m As Boolean
            Public Property Modified() As Boolean
                Get
                    Return _m
                End Get
                Set(ByVal value As Boolean)
                    _m = value
                End Set
            End Property

            Public Sub New(ByVal f As IFilter)
                _f = f
            End Sub
        End Class

        Public Event OnModifyFilter(ByVal sender As RelationCmd, ByVal args As ModifyFilter)

        Public Overrides Sub CopyTo(ByVal o As QueryCmd)
            MyBase.CopyTo(o)
            With CType(o, RelationCmd)
                ._rel = _rel
                ._desc = _desc
            End With
        End Sub

        Public Overrides Function Clone() As Object
            Dim q As New RelationCmd
            CopyTo(q)
            Return q
        End Function

        Protected Overrides Sub _Prepare(ByVal executor As IExecutor, _
            ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, _
            ByVal stmt As StmtGenerator, ByRef f As IFilter, ByVal selectOS As EntityUnion, ByVal isAnonym As Boolean)

            If selectOS Is Nothing Then
                Throw New QueryCmdException("RelationCmd must not select more than one type", Me)
            End If

            Dim _m2mObject As IKeyEntity = _rel.Host
            Dim _m2mKey As String = _rel.Key
            Dim selectType As Type = selectOS.GetRealType(schema)

            If Not AutoJoins Then
                Dim joins() As Worm.Criteria.Joins.QueryJoin = Nothing
                Dim appendMain_ As Boolean
                If OrmManager.HasJoins(schema, selectType, f, propSort, filterInfo, joins, appendMain_) Then
                    _js.AddRange(joins)
                End If
                AppendMain = AppendMain OrElse appendMain_

            End If

            If _m2mObject IsNot Nothing Then
                Dim selectedType As Type = selectType
                Dim filteredType As Type = _m2mObject.GetType

                Dim rel As RelationDesc = PrepareRel(schema, selectOS, selectType)

                Dim m2m As Boolean = TypeOf rel Is M2MRelationDesc

                Dim addf As IFilter = Nothing

                If m2m Then
                    'If SelectList IsNot Nothing AndAlso SelectList.Count > 0 Then
                    '    Throw New NotSupportedException("Cannot select individual column in m2m query")
                    'End If

                    Dim selected_r As M2MRelationDesc = CType(rel, M2MRelationDesc)
                    Dim filtered_r As M2MRelationDesc = schema.GetM2MRelation(selectedType, filteredType, _m2mKey)

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

                    If AppendMain OrElse _WithLoad(selectOS, schema) OrElse IsFTS Then
                        'table = CType(table.Clone, SourceFragment)
                        AppendMain = True
                        Dim jf As New JoinFilter(table, selected_r.Column, _
                            selectOS, schema.GetPrimaryKeys(selectedType)(0).PropertyAlias, _fo)
                        Dim jn As New QueryJoin(table, JoinType.Join, jf)
                        _js.Add(jn)
                        If _from Is Nothing OrElse table.Equals(_from.Table) Then
                            _from = New FromClauseDef(selectOS)
                        End If
                        If _WithLoad(selectOS, schema) Then
                            _sl.AddRange(schema.GetSortedFieldList(selectedType).ConvertAll(Function(c As EntityPropertyAttribute) ObjectMappingEngine.ConvertColumn2SelExp(c, selectOS)))
                        Else
                            GoTo l1
                        End If
                    Else
                        _from = New FromClauseDef(table)
                        'Dim os As IOrmObjectSchemaBase = schema.GetEntitySchema(selectedType)
                        'os.GetFieldColumnMap()("ID")._columnName
l1:
                        If SelectList IsNot Nothing Then
                            PrepareSelectList(isAnonym, schema, f, filterInfo)
                        Else
                            Dim pk As EntityPropertyAttribute = schema.GetPrimaryKeys(selectType)(0)
                            Dim se As New SelectExpression(table, selected_r.Column, pk.PropertyAlias)
                            se.Attributes = Field2DbRelations.PK
                            _sl.Add(se)
                        End If

                        'If SelectTypes(0).First.Equals(selectOS) Then
                        'Else
                        '    Throw New NotImplementedException
                        'End If
                    End If

                    'If SelectTypes.Count > 1 Then
                    '    For i As Integer = 1 To SelectTypes.Count - 1
                    '        AddTypeFields(schema, _sl, SelectTypes(i))
                    '    Next
                    'End If

                    addf = New TableFilter(table, filtered_r.Column, _
                        New Worm.Criteria.Values.ScalarValue(_m2mObject.Identifier), _fo)
                Else
                    If SelectList Is Nothing Then
                        If _WithLoad(selectOS, schema) Then
                            _sl.AddRange(schema.GetSortedFieldList(selectedType).ConvertAll(Function(c As EntityPropertyAttribute) ObjectMappingEngine.ConvertColumn2SelExp(c, selectOS)))
                        Else
                            Dim pk As EntityPropertyAttribute = schema.GetPrimaryKeys(selectType)(0)
                            Dim se As New SelectExpression(selectOS, pk.PropertyAlias)
                            se.Attributes = Field2DbRelations.PK
                            _sl.Add(se)
                        End If
                    Else
                        PrepareSelectList(isAnonym, schema, f, filterInfo)
                    End If

                    addf = New EntityFilter(rel.Rel, rel.Column, _
                        New Worm.Criteria.Values.ScalarValue(_m2mObject.Identifier), _fo)

                    If _from Is Nothing Then _from = New FromClauseDef(selectOS)
                End If

                Dim mf As New ModifyFilter(addf)
                RaiseEvent OnModifyFilter(Me, mf)

                If mf.Modified Then
                    addf = mf.RelFilter
                End If

                Dim con As Condition.ConditionConstructor = New Condition.ConditionConstructor
                con.AddFilter(f)
                con.AddFilter(addf)
                f = con.Condition

                _f = f
                Return
            End If
        End Sub

        Protected Function PrepareRel() As RelationDesc
            Return PrepareRel(Nothing, Nothing, Nothing)
        End Function

        Protected Function PrepareRel(ByVal schema As ObjectMappingEngine, _
            ByVal selectOS As EntityUnion, ByVal selectedType As Type) As RelationDesc

            Dim selRel As RelationDesc = _rel.Relation

            If selRel Is Nothing OrElse selRel.Rel Is Nothing OrElse String.IsNullOrEmpty(selRel.Column) Then
                Dim _m2mObject As IKeyEntity = _rel.Host
                Dim _m2mKey As String = _rel.Key

                Dim filteredType As Type = _m2mObject.GetType

                Dim needReplace As RelationDesc = Nothing
                Dim field As String = Nothing

                If selectOS Is Nothing Then
                    selectOS = GetSelectedOS()
                End If

                If schema Is Nothing Then
                    schema = _m2mObject.MappingEngine
                    If schema Is Nothing AndAlso _getMgr IsNot Nothing Then
                        Using mgr As OrmManager = _getMgr.CreateManager
                            schema = mgr.MappingEngine
                        End Using
                    End If

                    If schema Is Nothing Then
                        Throw New QueryCmdException(String.Format("Cannot get ObjectMappingEngine"), Me)
                    End If
                End If

                If selectedType Is Nothing Then
                    If selectOS Is Nothing Then
                        selectedType = selRel.Rel.GetRealType(schema)
                    Else
                        selectedType = selectOS.GetRealType(schema)
                    End If
                End If

                If _rel.Relation Is Nothing OrElse _rel.Relation.Rel Is Nothing OrElse String.IsNullOrEmpty(_rel.Relation.Column) Then
                    field = schema.GetJoinFieldNameByType(selectedType, filteredType, schema.GetEntitySchema(selectedType))
                    needReplace = New RelationDesc(selectOS, field)
                ElseIf Not TypeOf _rel.Relation Is M2MRelationDesc Then
                    field = _rel.Relation.Column
                End If

                If String.IsNullOrEmpty(field) Then
                    Dim revKey As String = _m2mKey
                    If selectedType Is filteredType Then
                        revKey = M2MRelationDesc.GetRevKey(_m2mKey)
                    End If
                    selRel = schema.GetM2MRelation(filteredType, selectedType, revKey)

                    If selRel Is Nothing Then
                        Throw New QueryCmdException(String.Format("Type {0} has no relation to {1}", selectedType.Name, filteredType.Name), Me)
                    Else
                        If _rel.Relation Is Nothing OrElse _rel.Relation.Rel Is Nothing OrElse String.IsNullOrEmpty(_rel.Relation.Column) Then
                            needReplace = selRel
                        ElseIf _rel.Relation.Rel.GetRealType(schema) IsNot selectedType Then
                            Throw New QueryCmdException(String.Format("Relation type is {0}, selected type is {1}", _rel.Relation.Rel.GetRealType(schema), selectedType), Me)
                        End If
                    End If
                End If

                If needReplace IsNot Nothing Then
                    Dim newRel As Relation = Nothing
                    If GetType(M2MRelationDesc).IsAssignableFrom(needReplace.GetType) Then
                        newRel = New M2MRelation(_rel.Host, CType(needReplace, M2MRelationDesc))
                    Else
                        newRel = New Relation(_rel.Host, needReplace)
                    End If
                    _m2mObject._ReplaceRel(_rel, newRel, schema)
                    _rel = newRel
                End If
            End If

            'If selRel Is Nothing Then
            '    Throw New ObjectMappingException(String.Format("Type {0} has no relation to {1}", selectedType.Name, filteredType.Name))
            'End If

            Return _rel.Relation
        End Function

        Public Overrides Property SelectTypes() As System.Collections.ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))
            Get
                Return MyBase.SelectTypes
            End Get
            Set(ByVal value As System.Collections.ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?)))
                If value IsNot Nothing Then
                    If value.Count > 1 Then
                        Throw New QueryCmdException("RelationCmd cant have more than one select type", Me)
                    End If
                End If
                MyBase.SelectTypes = value
            End Set
        End Property

        Public Function WithLoad(ByVal value As Boolean) As RelationCmd
            [Select](SelectTypes(0).First, value)
            Return Me
        End Function

        Public Sub LoadObjects()
            If _getMgr Is Nothing Then
                Throw New InvalidOperationException("OrmManager required")
            End If

            LoadObjects(_getMgr)
        End Sub

        Public Sub LoadObjects(ByVal getMgr As ICreateManager)
            Using mgr As OrmManager = getMgr.CreateManager
                Using New SetManagerHelper(mgr, getMgr)
                    LoadObjects(mgr)
                End Using
            End Using
        End Sub

        Public Sub LoadObjects(ByVal mgr As OrmManager)
            Dim os As EntityUnion = GetSelectedOS()
            If os Is Nothing Then
                Throw New QueryCmdException("You must select type", Me)
            End If
            If IsInCache(mgr) Then
                'Dim t As Type = os.GetRealType(mgr.MappingEngine)
                Dim i As IList = ToList(mgr)
                CType(i, ILoadableList).LoadObjects()
                'If GetType(KeyEntity).IsAssignableFrom(t) Then

                'Else
                'End If
            Else
                Dim s As New svct(Me)
                Using New OnExitScopeAction(AddressOf s.SetCT2Nothing)
                    WithLoad(True).ToList(mgr)
                End Using
            End If
        End Sub

        Public Sub LoadObjects(ByVal start As Integer, ByVal length As Integer)
            If _getMgr Is Nothing Then
                Throw New InvalidOperationException("OrmManager required")
            End If

            LoadObjects(_getMgr, start, length)
        End Sub

        Public Sub LoadObjects(ByVal getMgr As ICreateManager, ByVal start As Integer, ByVal length As Integer)
            Using mgr As OrmManager = getMgr.CreateManager
                Using New SetManagerHelper(mgr, getMgr)
                    LoadObjects(mgr, start, length)
                End Using
            End Using
        End Sub

        Public Sub LoadObjects(ByVal mgr As OrmManager, ByVal start As Integer, ByVal length As Integer)
            Dim os As EntityUnion = GetSelectedOS()
            If os Is Nothing Then
                Throw New QueryCmdException("You must select type", Me)
            End If
            If IsInCache(mgr) Then
                'Dim t As Type = os.GetRealType(mgr.MappingEngine)
                Dim i As IList = ToList(mgr)
                CType(i, ILoadableList).LoadObjects(start, length)
                'If GetType(KeyEntity).IsAssignableFrom(t) Then

                'Else
                'End If
            Else
                Dim s As New svct(Me)
                Using New OnExitScopeAction(AddressOf s.SetCT2Nothing)
                    If mgr.StmtGenerator.SupportRowNumber Then
                        Dim oldr As TableFilter = RowNumberFilter
                        Try
                            RowNumberFilter = New TableFilter(QueryCmd.RowNumerColumn, New BetweenValue(start + 1, start + length), Worm.Criteria.FilterOperation.Between)
                            WithLoad(True).ToList(mgr)
                        Finally
                            RowNumberFilter = oldr
                        End Try
                    Else
                        Dim t As Top = propTop
                        Try
                            Dim i As IList = WithLoad(True).Top(start + length).ToList(mgr)
                            CType(i, ILoadableList).LoadObjects(start, i.Count - start)
                        Finally
                            propTop = t
                        End Try
                    End If
                End Using
            End If
        End Sub

        Public Sub Add(ByVal o As IKeyEntity)
            If o IsNot Nothing Then
                Relation.Add(o)
                Using gm As IGetManager = o.GetMgr
                    Dim mpe As ObjectMappingEngine = gm.Manager.MappingEngine
                    mpe.SetPropertyValue(o, Relation.Relation.Column, Relation.Host, mpe.GetEntitySchema(o.GetType))
                End Using
            End If
        End Sub

        Public Sub Remove(ByVal o As IKeyEntity)
            Relation.Delete(o)
        End Sub

        Public Sub RemoveAll()
            PrepareRel()
            For Each o As IKeyEntity In ToList()
                Remove(o)
            Next
        End Sub

        Public Sub RemoveAll(ByVal mgr As OrmManager)
            PrepareRel()
            For Each o As IKeyEntity In ToList(mgr)
                Remove(o)
            Next
        End Sub

        Public Sub Merge(ByVal col As IList(Of IKeyEntity), ByVal removeNotInList As Boolean)
            Relation.Merge(Me, col, removeNotInList)
        End Sub

        Friend Sub SetCache(ByVal l As IEnumerable)
            If _getMgr Is Nothing Then
                Throw New InvalidOperationException("OrmManager required")
            End If

            Using mgr As OrmManager = GetMgr.CreateManager
                CType(GetExecutor(mgr), QueryExecutor).SetCache(mgr, Me, l)
            End Using
        End Sub
    End Class
End Namespace
