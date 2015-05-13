Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Cache
Imports Worm.Criteria.Core
Imports Worm.Criteria.Conditions
Imports Worm.Criteria.Joins
Imports Worm.Criteria.Values
Imports System.Collections.Generic
Imports Worm.Expressions2

Namespace Query
    Public Class RelationCmd
        Inherits QueryCmd

        Friend _rel As Relation
        Private _desc As RelationDesc
        Private _fo As Criteria.FilterOperation = Criteria.FilterOperation.Equal
        Private _withHost As Boolean

#Region " Ctors "
        Protected Sub New()
            Init()
        End Sub

        Public Sub New(ByVal rel As Relation)
            _rel = rel
            [From](rel.Relation.Entity)
            Init()
        End Sub

        Public Sub New(ByVal desc As RelationDesc, ByVal getMgr As CreateManagerDelegate)
            MyBase.New(getMgr)
            _desc = desc
            [From](desc.Entity)
            Init()
        End Sub

        Public Sub New(ByVal desc As RelationDesc)
            _desc = desc
            [From](desc.Entity)
            Init()
        End Sub

        Public Sub New(ByVal rel As Relation, ByVal getMgr As CreateManagerDelegate)
            MyBase.New(getMgr)
            _rel = rel
            [From](rel.Relation.Entity)
            Init()
        End Sub

        Public Sub New(ByVal obj As ISinglePKEntity, ByVal t As Type)
            'MyBase.New(obj)
            If _desc Is Nothing Then
                _rel = New Relation(obj, New RelationDesc(New EntityUnion(t), Nothing))
            Else
                _rel = New Relation(obj, _desc)
            End If
            [From](_rel.Relation.Entity)
            Init()
        End Sub

        Public Sub New(ByVal obj As ISinglePKEntity, ByVal t As Type, ByVal getMgr As ICreateManager)
            MyBase.New(getMgr)
            If _desc Is Nothing Then
                _rel = New Relation(obj, New RelationDesc(New EntityUnion(t), Nothing))
            Else
                _rel = New Relation(obj, _desc)
            End If
            [From](_rel.Relation.Entity)
            Init()
        End Sub

        Public Sub New(ByVal obj As ISinglePKEntity, ByVal eu As EntityUnion)
            'MyBase.New(obj)
            If _desc Is Nothing Then
                _rel = New Relation(obj, New RelationDesc(eu, Nothing))
            Else
                _rel = New Relation(obj, _desc)
            End If
            [From](_rel.Relation.Entity)
            Init()
        End Sub

        Public Sub New(ByVal obj As ISinglePKEntity, ByVal eu As EntityUnion, ByVal key As String)
            'MyBase.New(obj, key)
            If _desc Is Nothing Then
                _rel = New Relation(obj, New RelationDesc(eu, Nothing, key))
            Else
                _rel = New Relation(obj, _desc)
            End If
            [From](_rel.Relation.Entity)
            Init()
        End Sub

        Public Sub New(ByVal desc As RelationDesc, ByVal obj As ISinglePKEntity)
            'MyBase.New(obj, desc.Key)
            _rel = New Relation(obj, desc)
            [From](desc.Entity)
            Init()
        End Sub

        Public Sub New(ByVal rel As Relation, ByVal getMgr As ICreateManager)
            MyBase.New(getMgr)
            _rel = rel
            [From](rel.Relation.Entity)
            Init()
        End Sub

        Public Sub New(ByVal desc As RelationDesc, ByVal getMgr As ICreateManager)
            MyBase.New(getMgr)
            _desc = desc
            [From](desc.Entity)
            Init()
        End Sub

        Public Sub New(ByVal obj As _ISinglePKEntity, ByVal eu As EntityUnion, ByVal getMgr As CreateManagerDelegate)
            MyBase.New(getMgr)
            If _desc Is Nothing Then
                _rel = New Relation(obj, New RelationDesc(eu, Nothing, Nothing))
            Else
                _rel = New Relation(obj, _desc)
            End If
            [From](_rel.Relation.Entity)
            Init()
        End Sub

        Public Sub New(ByVal obj As _ISinglePKEntity, ByVal eu As EntityUnion, ByVal getMgr As ICreateManager)
            MyBase.New(getMgr)
            If _desc Is Nothing Then
                _rel = New Relation(obj, New RelationDesc(eu, Nothing, Nothing))
            Else
                _rel = New Relation(obj, _desc)
            End If
            [From](_rel.Relation.Entity)
            Init()
        End Sub

        Public Sub New(ByVal obj As _ISinglePKEntity, ByVal eu As EntityUnion, ByVal key As String, ByVal getMgr As ICreateManager)
            MyBase.New(getMgr)
            If _desc Is Nothing Then
                _rel = New Relation(obj, New RelationDesc(eu, Nothing, key))
            Else
                _rel = New Relation(obj, _desc)
            End If
            [From](_rel.Relation.Entity)
            Init()
        End Sub

        Public Sub New(ByVal desc As RelationDesc, ByVal obj As _ISinglePKEntity, ByVal getMgr As ICreateManager)
            MyBase.New(getMgr)
            _rel = New Relation(obj, desc)
            [From](desc.Entity)
            Init()
        End Sub
#End Region

#Region " Create "
        Public Overloads Shared Function Create(ByVal desc As RelationDesc) As RelationCmd
            Return Create(desc, OrmManager.CurrentManager)
        End Function

        Public Overloads Shared Function Create(ByVal rel As Relation) As RelationCmd
            Return Create(rel, OrmManager.CurrentManager)
        End Function

        Public Overloads Shared Function Create(ByVal obj As ISinglePKEntity, ByVal en As EntityUnion) As RelationCmd
            Return Create(obj, en, OrmManager.CurrentManager)
        End Function

        Public Overloads Shared Function Create(ByVal obj As ISinglePKEntity, ByVal en As EntityUnion, ByVal key As String) As RelationCmd
            Return Create(obj, en, key, OrmManager.CurrentManager)
        End Function

        Public Overloads Shared Function Create(ByVal name As String, ByVal obj As ISinglePKEntity, ByVal en As EntityUnion) As RelationCmd
            Return Create(name, obj, en, OrmManager.CurrentManager)
        End Function

        Public Overloads Shared Function Create(ByVal name As String, ByVal obj As ISinglePKEntity, ByVal en As EntityUnion, ByVal key As String) As RelationCmd
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

        Public Overloads Shared Function Create(ByVal obj As ISinglePKEntity, ByVal en As EntityUnion, ByVal mgr As OrmManager) As RelationCmd
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

        Public Overloads Shared Function Create(ByVal obj As ISinglePKEntity, ByVal en As EntityUnion, ByVal key As String, ByVal mgr As OrmManager) As RelationCmd
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

        Public Overloads Shared Function Create(ByVal name As String, ByVal obj As ISinglePKEntity, ByVal en As EntityUnion, ByVal mgr As OrmManager) As RelationCmd
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

        Public Overloads Shared Function Create(ByVal name As String, ByVal obj As ISinglePKEntity, ByVal en As EntityUnion, ByVal key As String, ByVal mgr As OrmManager) As RelationCmd
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

        Protected Overrides Function _Clone() As Object
            Dim q As New RelationCmd
            CopyTo(q)
            Return q
        End Function

        Protected Sub Init()
            AddHandler ModifyResult, AddressOf _ModifyResult
        End Sub

        Protected Overrides Sub _Prepare(ByVal executor As IExecutor, _
            ByVal mpe As ObjectMappingEngine, ByVal filterInfo As IDictionary, _
            ByVal stmt As StmtGenerator, ByRef f As IFilter, ByVal xxx As EntityUnion, ByVal isAnonym As Boolean)

            'If selectOS Is Nothing Then
            '    Throw New QueryCmdException("RelationCmd must not select more than one type", Me)
            'End If

            Dim m2mObject As ISinglePKEntity = _rel.Host
            Dim m2mKey As String = _rel.Key
            'Dim m2mType As Type = selectOS.GetRealType(schema)
            Dim rel As RelationDesc = PrepareRel(mpe, Nothing, Nothing)
            Dim m2mEU As EntityUnion = rel.Entity
            Dim m2mType As Type = m2mEU.GetRealType(mpe)

            If Not AutoJoins Then
                Dim joins() As Worm.Criteria.Joins.QueryJoin = Nothing
                Dim appendMain_ As Boolean
                If HasJoins(mpe, m2mType, f, Sort, filterInfo, joins, appendMain_, m2mEU) Then
                    For Each j As QueryJoin In joins
                        If Not HasInQueryJS(j.ObjectSource) Then
                            _js.Add(j)
                        End If
                    Next
                End If
                AppendMain = AppendMain OrElse appendMain_
            End If

            If m2mObject IsNot Nothing Then
                Dim hostType As Type = m2mObject.GetType

                Dim m2m As Boolean = TypeOf rel Is M2MRelationDesc

                Dim addf As IFilter = Nothing

                Dim oschema As IEntitySchema = mpe.GetEntitySchema(m2mType)

                If m2m Then
                    Dim selected_r As M2MRelationDesc = CType(rel, M2MRelationDesc)
                    Dim filtered_r As M2MRelationDesc = Nothing
                    If hostType Is m2mType Then
                        filtered_r = mpe.GetM2MRelation(oschema, hostType, M2MRelationDesc.GetRevKey(m2mKey))
                    Else
                        filtered_r = mpe.GetM2MRelation(oschema, hostType, m2mKey)
                    End If

                    If filtered_r Is Nothing Then
                        Dim en As String = mpe.GetEntityNameByType(hostType)
                        If String.IsNullOrEmpty(en) Then
                            Throw New ObjectMappingException(String.Format("Type {0} has no relation to {1}", hostType.Name, m2mType.Name))
                        End If

                        filtered_r = mpe.GetM2MRelation(m2mType, mpe.GetTypeByEntityName(en), m2mKey)

                        If filtered_r Is Nothing Then
                            Throw New ObjectMappingException(String.Format("Type {0} has no relation to {1}", hostType.Name, m2mType.Name))
                        End If
                    End If

                    'Dim table As SourceFragment = selected_r.Table
                    Dim table As SourceFragment = filtered_r.Table

                    If table Is Nothing Then
                        Throw New ArgumentException("Invalid relation", hostType.ToString)
                    End If

                    Dim ideu As EntityUnion = m2mEU
                    Dim tu As EntityUnion = Nothing
                    Dim mt As IMultiTableObjectSchema = TryCast(oschema, IMultiTableObjectSchema)
                    Dim prd As Boolean = (AppendMain.HasValue AndAlso AppendMain.Value) OrElse _WithLoad(m2mEU, mpe) OrElse IsFTS OrElse
                        NeedAppendMain(m2mEU)
                    If prd OrElse mt IsNot Nothing Then
                        Dim pka As String = mpe.GetSinglePK(m2mType, oschema)
                        AppendMain = True
                        Dim jf As New JoinFilter(table, selected_r.Column, m2mEU, pka, _fo)
                        Dim jn As New QueryJoin(table, JoinType.Join, jf)
                        jn.ObjectSource = selected_r.Entity
                        _js.Insert(0, jn)
                        If _from Is Nothing OrElse table.Equals(_from.Table) Then
                            _from = New FromClauseDef(m2mEU)
                        End If
                        tu = m2mEU

                        If _WithLoad(m2mEU, mpe) Then
                            For Each mp As MapField2Column In oschema.FieldColumnMap
                                _sl.Add(New SelectExpression(New ObjectProperty(m2mEU, mp.PropertyAlias)))
                            Next
                            '_sl.AddRange(mpe.GetSortedFieldList(m2mType).ConvertAll(Function(c As EntityPropertyAttribute) ObjectMappingEngine.ConvertColumn2SelExp(c, m2mEU)))
                        ElseIf SelectedEntities IsNot Nothing Then
                            GoTo l1
                        Else
                            PrepareSelectList(executor, stmt, isAnonym, mpe, f, filterInfo)
                        End If

                    Else
                        _from = New FromClauseDef(table)
                        ideu = Nothing
l1:
                        If SelectList IsNot Nothing Then
                            PrepareSelectList(executor, stmt, isAnonym, mpe, f, filterInfo)
                        Else
                            If SelectedEntities IsNot Nothing AndAlso Not SelectedEntities(0).First.Equals(m2mEU) Then
                                'se.ObjectSource = SelectTypes(0).First
                                AddTypeFields(mpe, _sl, SelectedEntities(0), Nothing, isAnonym, stmt)
                                'Dim selt As EntityUnion = SelectTypes(0).First
                            Else
                                'Dim pk As EntityPropertyAttribute = mpe.GetPrimaryKeys(m2mType)(0)
                                Dim pka As String = mpe.GetSinglePK(m2mType, oschema)
                                Dim te As New TableExpression(table, selected_r.Column)
                                te.SetEntity(ideu)
                                _sl.Add(New SelectExpression(te) With { _
                                    .Attributes = Field2DbRelations.PK, _
                                    .IntoPropertyAlias = pka, _
                                    .Into = m2mEU _
                                })
                            End If
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
                        New Worm.Criteria.Values.ScalarValue(m2mObject.Identifier), _fo)

                    If selected_r.Constants IsNot Nothing Then
                        Dim cond As Condition.ConditionConstructor = New Condition.ConditionConstructor
                        cond.AddFilter(addf)
                        For Each tf As IFilter In selected_r.Constants
                            cond.AddFilter(tf)
                        Next
                        addf = cond.Condition
                    End If

                    If tu IsNot Nothing Then addf.SetUnion(tu)

                    If Not _types.ContainsKey(m2mEU) Then _types.Add(m2mEU, oschema)

                    If QueryWithHost Then
                        Dim heu As New EntityUnion(hostType)
                        Dim hschema As IEntitySchema = mpe.GetEntitySchema(hostType)
                        _types.Add(heu, hschema)
                        Dim hpk As String = mpe.GetSinglePK(hostType, hschema) 'mpe.GetPrimaryKeys(hostType, hschema)(0).PropertyAlias
                        _sl.Add(New SelectExpression(table, filtered_r.Column, hpk, heu))

                        AddEntityToSelectList(heu, False)

                        '_pdic.Add(m2mType, mpe.GetProperties(m2mType, oschema))
                        '_pdic.Add(hostType, mpe.GetProperties(hostType, hschema))
                    End If

                Else
                    If SelectList Is Nothing Then
                        If _WithLoad(m2mEU, mpe) Then
                            For Each mp As MapField2Column In oschema.FieldColumnMap
                                _sl.Add(New SelectExpression(New ObjectProperty(m2mEU, mp.PropertyAlias)))
                            Next
                            '_sl.AddRange(mpe.GetSortedFieldList(m2mType).ConvertAll(Function(c As EntityPropertyAttribute) ObjectMappingEngine.ConvertColumn2SelExp(c, m2mEU)))
                        Else
                            If SelectedEntities IsNot Nothing AndAlso Not SelectedEntities(0).First.Equals(m2mEU) Then
                                'se.ObjectSource = SelectTypes(0).First
                                AddTypeFields(mpe, _sl, SelectedEntities(0), Nothing, isAnonym, stmt)
                                'Dim selt As EntityUnion = SelectTypes(0).First
                            Else
                                For Each mp As MapField2Column In oschema.FieldColumnMap
                                    If mp.IsPK Then
                                        _sl.Add(New SelectExpression(New ObjectProperty(m2mEU, mp.PropertyAlias)))
                                    End If
                                Next
                            End If
                        End If

                        If Not _types.ContainsKey(m2mEU) Then
                            _types.Add(m2mEU, oschema)
                        End If
                    Else
                        If Not _types.ContainsKey(m2mEU) Then
                            _types.Add(m2mEU, oschema)
                        End If

                        PrepareSelectList(executor, stmt, isAnonym, mpe, f, filterInfo)
                    End If

                    addf = New EntityFilter(rel.Entity, rel.Column, _
                           New Worm.Criteria.Values.ScalarValue(m2mObject.Identifier), _fo)

                    If _from Is Nothing Then _from = New FromClauseDef(m2mEU)
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
            End If


        End Sub

        Protected Function PrepareRel() As RelationDesc
            Return PrepareRel(Nothing, Nothing, Nothing)
        End Function

        Protected Function PrepareRel(ByVal schema As ObjectMappingEngine, _
            ByVal selectOS As EntityUnion, ByVal selectedType As Type) As RelationDesc

            Dim selRel As RelationDesc = _rel.Relation

            If selRel Is Nothing OrElse selRel.Entity Is Nothing OrElse String.IsNullOrEmpty(selRel.Column) Then
                Dim m2mObject As ISinglePKEntity = _rel.Host
                Dim m2mKey As String = _rel.Key

                Dim filteredType As Type = m2mObject.GetType

                Dim needReplace As RelationDesc = Nothing
                Dim field As String = Nothing

                'If selectOS Is Nothing Then
                '    selectOS = GetSelectedOS()
                'End If

                If schema Is Nothing Then
                    schema = m2mObject.GetMappingEngine
                    If schema Is Nothing AndAlso _getMgr IsNot Nothing Then
                        Using gm = GetManager(_getMgr)
                            schema = gm.Manager.MappingEngine
                        End Using
                    End If

                    If schema Is Nothing Then
                        Throw New QueryCmdException(String.Format("Cannot get ObjectMappingEngine"), Me)
                    End If
                End If

                If selectedType Is Nothing Then
                    If selectOS Is Nothing Then
                        selectOS = selRel.Entity
                    End If
                    selectedType = selectOS.GetRealType(schema)
                End If

                If _rel.Relation Is Nothing OrElse _rel.Relation.Entity Is Nothing OrElse String.IsNullOrEmpty(_rel.Relation.Column) Then
                    Dim oschema As IEntitySchema = schema.GetEntitySchema(selectedType)
                    field = schema.GetJoinFieldNameByType(filteredType, oschema)
                    needReplace = New RelationDesc(selectOS, field)
                ElseIf Not TypeOf _rel.Relation Is M2MRelationDesc Then
                    field = _rel.Relation.Column
                End If

                If String.IsNullOrEmpty(field) Then
                    'Dim revKey As String = _m2mKey
                    'If selectedType Is filteredType Then
                    '    revKey = M2MRelationDesc.GetRevKey(_m2mKey)
                    'End If
                    selRel = schema.GetM2MRelation(filteredType, selectedType, m2mKey)

                    If selRel Is Nothing Then
                        Throw New QueryCmdException(String.Format("Type {0} has no relation to {1}", selectedType.Name, filteredType.Name), Me)
                    Else
                        If _rel.Relation Is Nothing OrElse _rel.Relation.Entity Is Nothing OrElse String.IsNullOrEmpty(_rel.Relation.Column) Then
                            needReplace = selRel
                            If _rel.Relation IsNot Nothing Then
                                needReplace.Entity = _rel.Relation.Entity
                            End If
                        ElseIf _rel.Relation.Entity.GetRealType(schema) IsNot selectedType Then
                            Throw New QueryCmdException(String.Format("Relation type is {0}, selected type is {1}", _rel.Relation.Entity.GetRealType(schema), selectedType), Me)
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
                    newRel.Added.AddRange(_rel.Added)
                    newRel.Deleted.AddRange(_rel.Deleted)
                    _rel = m2mObject.NormalizeRelation(_rel, newRel, schema)
                End If
            End If

            'If selRel Is Nothing Then
            '    Throw New ObjectMappingException(String.Format("Type {0} has no relation to {1}", selectedType.Name, filteredType.Name))
            'End If

            Return _rel.Relation
        End Function

        Public Overrides Property SelectedEntities() As System.Collections.ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?))
            Get
                Return MyBase.SelectedEntities
            End Get
            Set(ByVal value As System.Collections.ObjectModel.ReadOnlyCollection(Of Pair(Of EntityUnion, Boolean?)))
                If value IsNot Nothing Then
                    If value.Count > 1 AndAlso Not QueryWithHost Then
                        Throw New QueryCmdException("RelationCmd cant have more than one select type", Me)
                    End If
                End If
                MyBase.SelectedEntities = value
            End Set
        End Property

        Public Function WithLoad(ByVal value As Boolean) As RelationCmd
            SelectEntity(SelectedEntities(0).First, value)
            Return Me
        End Function

        Public Overloads Sub LoadObjects()
            If _getMgr Is Nothing Then
                Throw New InvalidOperationException("OrmManager required")
            End If

            LoadObjects(_getMgr)
        End Sub

        Public Overloads Sub LoadObjects(ByVal getMgr As ICreateManager)
            Using gm = GetManager(getMgr)
                Using New SetManagerHelper(gm.Manager, getMgr, SpecificMappingEngine, ContextInfo)
                    LoadObjects(gm.Manager)
                End Using
            End Using
        End Sub

        Public Overloads Sub LoadObjects(ByVal mgr As OrmManager)
            Dim os As EntityUnion = GetSelectedOS()
            If os Is Nothing Then
                Throw New QueryCmdException("You must select type", Me)
            End If
            If IsInCache(mgr) Then
                'Dim t As Type = os.GetRealType(mgr.MappingEngine)
                Dim i As IList = ToList(mgr)
                CType(i, ILoadableList).LoadObjects()
                'If GetType(SinglePKEntity).IsAssignableFrom(t) Then

                'Else
                'End If
            Else
                Dim s As New svct(Me)
                Using New OnExitScopeAction(AddressOf s.SetCT2Nothing)
                    WithLoad(True).ToList(mgr)
                End Using
            End If
        End Sub

        Public Overloads Sub LoadObjects(ByVal start As Integer, ByVal length As Integer)
            If _getMgr Is Nothing Then
                Throw New InvalidOperationException("OrmManager required")
            End If

            LoadObjects(_getMgr, start, length)
        End Sub

        Public Overloads Sub LoadObjects(ByVal getMgr As ICreateManager, ByVal start As Integer, ByVal length As Integer)
            Using gm = GetManager(getMgr)
                Using New SetManagerHelper(gm.Manager, getMgr, SpecificMappingEngine, ContextInfo)
                    LoadObjects(gm.Manager, start, length)
                End Using
            End Using
        End Sub

        Public Overloads Sub LoadObjects(ByVal mgr As OrmManager, ByVal start As Integer, ByVal length As Integer)
            Dim os As EntityUnion = GetSelectedOS()
            If os Is Nothing Then
                Throw New QueryCmdException("You must select type", Me)
            End If
            If IsInCache(mgr) Then
                'Dim t As Type = os.GetRealType(mgr.MappingEngine)
                Dim i As IList = ToList(mgr)
                CType(i, ILoadableList).LoadObjects(start, length)
                'If GetType(SinglePKEntity).IsAssignableFrom(t) Then

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
                        Dim t As Top = TopParam
                        Try
                            Dim i As IList = WithLoad(True).Top(start + length).ToList(mgr)
                            CType(i, ILoadableList).LoadObjects(start, i.Count - start)
                        Finally
                            TopParam = t
                        End Try
                    End If
                End Using
            End If
        End Sub

        'Public Sub Add(ByVal o As ICachedEntity, ByVal key As String)
        '    If o IsNot Nothing Then
        '        Relation.Host.Add(o, key)
        '        If Not IsM2M Then
        '            Using gm As IGetManager = o.GetMgr
        '                Dim mpe As ObjectMappingEngine = gm.Manager.MappingEngine
        '                Try
        '                    mpe.SetPropertyValue(o, Relation.Relation.Column, Relation.Host, mpe.GetEntitySchema(o.GetType))
        '                Catch ex As InvalidCastException
        '                    mpe.SetPropertyValue(o, Relation.Relation.Column, Relation.Host.Identifier, mpe.GetEntitySchema(o.GetType))
        '                End Try
        '            End Using
        '        End If
        '    End If
        'End Sub

        Public Sub Add(ByVal o As ICachedEntity)
            If o IsNot Nothing Then
                Relation.Host.Add(o, Relation)
                If Not IsM2M Then
                    Dim mpe As ObjectMappingEngine = o.GetMappingEngine()
                    If mpe Is Nothing Then
                        mpe = Relation.Host.GetMappingEngine
                    End If

                    Dim schema As IEntitySchema = Nothing
                    If mpe IsNot Nothing Then
                        schema = mpe.GetEntitySchema(o.GetType)
                    End If

                    Try
                        Try
                            ObjectMappingEngine.SetPropertyValue(o, Relation.Relation.Column, Relation.Host, schema)
                        Catch ex As InvalidCastException
                            ObjectMappingEngine.SetPropertyValue(o, Relation.Relation.Column, Relation.Host.Identifier, schema)
                        End Try
                    Catch ex As ArgumentNullException When ex.Message = "oschema"
                        schema = o.GetEntitySchema(mpe)
                        Try
                            ObjectMappingEngine.SetPropertyValue(o, Relation.Relation.Column, Relation.Host, schema)
                        Catch ex2 As InvalidCastException
                            ObjectMappingEngine.SetPropertyValue(o, Relation.Relation.Column, Relation.Host.Identifier, schema)
                        End Try
                    End Try
                End If
            End If
        End Sub

        'Public Sub Remove(ByVal o As ICachedEntity, ByVal key As String)
        '    Relation.Host.Remove(o, key)
        '    If Not IsM2M Then
        '        Using gm As IGetManager = o.GetMgr
        '            Dim mpe As ObjectMappingEngine = gm.Manager.MappingEngine
        '            Try
        '                mpe.SetPropertyValue(o, Relation.Relation.Column, Nothing, mpe.GetEntitySchema(o.GetType))
        '            Catch ex As InvalidCastException
        '                mpe.SetPropertyValue(o, Relation.Relation.Column, 0, mpe.GetEntitySchema(o.GetType))
        '            End Try
        '        End Using
        '    End If
        'End Sub

        Public Sub Remove(ByVal o As ICachedEntity)
            Relation.Host.Remove(o, Relation)
            If Not IsM2M Then
                Dim mpe As ObjectMappingEngine = o.GetMappingEngine()
                If mpe Is Nothing Then
                    mpe = Relation.Host.GetMappingEngine
                End If

                Dim schema As IEntitySchema = Nothing
                If mpe IsNot Nothing Then
                    schema = mpe.GetEntitySchema(o.GetType)
                End If

                Try
                    Try
                        ObjectMappingEngine.SetPropertyValue(o, Relation.Relation.Column, Nothing, schema)
                    Catch ex As InvalidCastException
                        ObjectMappingEngine.SetPropertyValue(o, Relation.Relation.Column, 0, schema)
                    End Try
                Catch ex As ArgumentNullException When ex.Message = "oschema"
                    schema = o.GetEntitySchema(mpe)
                    Try
                        ObjectMappingEngine.SetPropertyValue(o, Relation.Relation.Column, Nothing, schema)
                    Catch ex2 As InvalidCastException
                        ObjectMappingEngine.SetPropertyValue(o, Relation.Relation.Column, 0, schema)
                    End Try
                End Try
            End If
        End Sub

        Public Sub RemoveAll()
            For Each o As ISinglePKEntity In ToList()
                Remove(o)
            Next
        End Sub

        Public Sub RemoveAll(ByVal mgr As OrmManager)
            For Each o As ISinglePKEntity In ToList(mgr)
                Remove(o)
            Next
        End Sub

        Public Sub Reject(ByVal mgr As OrmManager)
            Relation.Reject(mgr)
        End Sub

        Public Sub Reject()
            Using gm = GetManager(_getMgr)
                Reject(gm.Manager)
            End Using
        End Sub

        Public Sub Merge(ByVal col As IList(Of ICachedEntity), ByVal removeNotInList As Boolean)
            Relation.Merge(Me, col, removeNotInList)
        End Sub

        Public ReadOnly Property IsM2M() As Boolean
            Get
                PrepareRel()
                Return TypeOf _rel.Relation Is M2MRelationDesc
            End Get
        End Property

        Public Property QueryWithHost() As Boolean
            Get
                Return _withHost
            End Get
            Set(ByVal value As Boolean)
                _withHost = value
            End Set
        End Property

        Public ReadOnly Property HasChanges() As Boolean
            Get
                Return Relation.HasChanges
            End Get
        End Property

        Public Overloads Overrides Function Count(ByVal mgr As OrmManager) As Integer
            Dim cnt As Integer = MyBase.Count(mgr)
            If Relation.HasChanges Then
                cnt -= mgr.ApplyFilter(GetSelectedType(mgr.MappingEngine), Relation.Deleted, Filter).Count
                cnt += mgr.ApplyFilter(GetSelectedType(mgr.MappingEngine), Relation.Added, Filter).Count
            End If
            Return cnt
        End Function

        Private _includeModified As Boolean
        Public Overrides Property IncludeModifiedObjects As Boolean
            Get
                Return _includeModified
            End Get
            Set(value As Boolean)
                _includeModified = value
            End Set
        End Property

        Protected Overrides Sub _ModifyResult(ByVal sender As QueryCmd, ByVal args As ModifyResultArgs)
            If _includeModified Then
                MyBase._ModifyResult(sender, args)
            End If

            If Relation.HasChanges AndAlso Not args.IsSimple Then

                Dim nr As IListEdit = CType(args.ReadOnlyList.Clone, IListEdit)

                For Each o As IEntity In args.OrmManager.ApplyFilter(args.ReadOnlyList.RealType, Relation.Deleted, Filter)
                    nr.Remove(o)
                Next

                Dim toAdd As New List(Of IEntity)
                For Each a As IEntity In args.OrmManager.ApplyFilter(args.ReadOnlyList.RealType, Relation.Added, Filter)
                    If Not nr.Contains(a) Then
                        toAdd.Add(a)
                    End If
                Next

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

        Private Function NeedAppendMain(m2mEU As EntityUnion) As Boolean
            If Filter IsNot Nothing Then
                For Each f As IFilter In Filter.Filter.GetAllFilters
                    Dim ef As EntityFilter = TryCast(f, EntityFilter)
                    If ef IsNot Nothing Then
                        If m2mEU.Equals(ef.Template.ObjectSource) Then
                            Return True
                        End If
                    Else
                        Dim join As JoinFilter = TryCast(f, JoinFilter)
                        If join IsNot Nothing Then
                            If m2mEU.Equals(join.Left.Property.Entity) Then
                                Return True
                            ElseIf m2mEU.Equals(join.Right.Property.Entity) Then
                                Return True
                            End If
                        End If
                    End If
                Next
            End If

            If SelectClause IsNot Nothing AndAlso SelectClause.SelectList IsNot Nothing Then
                For Each e As IExpression In SelectClause.SelectList
                    For Each e2 As IExpression In e.GetExpressions
                        Dim ee As EntityExpression = TryCast(e2, EntityExpression)
                        If ee IsNot Nothing Then
                            If m2mEU.Equals(ee.ObjectProperty.Entity) Then
                                Return True
                            End If
                        End If
                    Next
                Next
            End If
            Return False
        End Function

    End Class
End Namespace
