Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Cache
Imports Worm.Criteria.Core
Imports Worm.Criteria.Conditions
Imports Worm.Criteria.Joins

Namespace Query
    Public Class RelationCmd
        Inherits QueryCmd

        Private _rel As Relation
        Private _desc As RelationDesc

#Region " Ctors "
        Public Sub New(ByVal rel As Relation)
            _rel = rel
        End Sub

        Public Sub New(ByVal desc As RelationDesc, ByVal getMgr As CreateManagerDelegate)
            MyBase.New(getMgr)
            _desc = desc
        End Sub

        Public Sub New(ByVal desc As RelationDesc)
            _desc = desc
        End Sub

        Public Sub New(ByVal rel As Relation, ByVal getMgr As CreateManagerDelegate)
            MyBase.New(getMgr)
            _rel = rel
        End Sub

        Public Sub New(ByVal obj As IKeyEntity)
            'MyBase.New(obj)
            If _desc Is Nothing Then
                _rel = New Relation(obj, CType(Nothing, RelationDesc))
            Else
                _rel = New Relation(obj, _desc)
            End If
        End Sub

        Public Sub New(ByVal obj As IKeyEntity, ByVal key As String)
            'MyBase.New(obj, key)
            If _desc Is Nothing Then
                _rel = New Relation(obj, New RelationDesc(Nothing, Nothing, key))
            Else
                _rel = New Relation(obj, _desc)
            End If
        End Sub

        Public Sub New(ByVal desc As RelationDesc, ByVal obj As IKeyEntity)
            'MyBase.New(obj, desc.Key)
            _rel = New Relation(obj, desc)
        End Sub

        Public Sub New(ByVal desc As RelationDesc, ByVal getMgr As ICreateManager)
            MyBase.New(getMgr)
            _desc = desc
        End Sub

        Public Sub New(ByVal obj As _IKeyEntity, ByVal getMgr As ICreateManager)
            MyBase.New(getMgr)
            If _desc Is Nothing Then
                _rel = New Relation(obj, New RelationDesc(Nothing, Nothing, Nothing))
            Else
                _rel = New Relation(obj, _desc)
            End If
        End Sub

        Public Sub New(ByVal obj As _IKeyEntity, ByVal key As String, ByVal getMgr As ICreateManager)
            MyBase.New(getMgr)
            If _desc Is Nothing Then
                _rel = New Relation(obj, New RelationDesc(Nothing, Nothing, key))
            Else
                _rel = New Relation(obj, _desc)
            End If
        End Sub

        Public Sub New(ByVal desc As RelationDesc, ByVal obj As _IKeyEntity, ByVal getMgr As ICreateManager)
            MyBase.New(getMgr)
            _rel = New Relation(obj, desc)
        End Sub
#End Region

#Region " Create "
        Public Overloads Shared Function Create(ByVal obj As IKeyEntity) As RelationCmd
            Return Create(obj, OrmManager.CurrentManager)
        End Function

        Public Overloads Shared Function Create(ByVal obj As IKeyEntity, ByVal key As String) As RelationCmd
            Return Create(obj, key, OrmManager.CurrentManager)
        End Function

        Public Overloads Shared Function Create(ByVal name As String, ByVal obj As IKeyEntity) As RelationCmd
            Return Create(name, obj, OrmManager.CurrentManager)
        End Function

        Public Overloads Shared Function Create(ByVal name As String, ByVal obj As IKeyEntity, ByVal key As String) As RelationCmd
            Return Create(name, obj, key, OrmManager.CurrentManager)
        End Function

        Public Overloads Shared Function Create(ByVal obj As IKeyEntity, ByVal mgr As OrmManager) As RelationCmd
            Dim f As ICreateQueryCmd = TryCast(mgr, ICreateQueryCmd)
            Dim q As RelationCmd = Nothing
            If f Is Nothing Then
                q = New RelationCmd(obj)
            Else
                q = f.Create(obj)
            End If
            Dim cm As ICreateManager = TryCast(mgr, ICreateManager)
            If cm IsNot Nothing Then
                q._getMgr = cm
            End If
            Return q
        End Function

        Public Overloads Shared Function Create(ByVal obj As IKeyEntity, ByVal key As String, ByVal mgr As OrmManager) As RelationCmd
            Dim f As ICreateQueryCmd = TryCast(mgr, ICreateQueryCmd)
            Dim q As RelationCmd = Nothing
            If f Is Nothing Then
                q = New RelationCmd(obj, key)
            Else
                q = f.Create(obj, key)
            End If
            Dim cm As ICreateManager = TryCast(mgr, ICreateManager)
            If cm IsNot Nothing Then
                q._getMgr = cm
            End If
            Return q
        End Function

        Public Overloads Shared Function Create(ByVal name As String, ByVal obj As IKeyEntity, ByVal mgr As OrmManager) As RelationCmd
            Dim f As ICreateQueryCmd = TryCast(mgr, ICreateQueryCmd)
            Dim q As RelationCmd = Nothing
            If f Is Nothing Then
                q = New RelationCmd(obj)
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

        Public Overloads Shared Function Create(ByVal name As String, ByVal obj As IKeyEntity, ByVal key As String, ByVal mgr As OrmManager) As RelationCmd
            Dim f As ICreateQueryCmd = TryCast(mgr, ICreateQueryCmd)
            Dim q As RelationCmd = Nothing
            If f Is Nothing Then
                q = New RelationCmd(obj, key)
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
        Public ReadOnly Property Relation() As Relation
            Get
                Return _rel
            End Get
        End Property
#End Region

        Protected Overrides Sub _Prepare(ByVal executor As IExecutor, _
            ByVal schema As ObjectMappingEngine, ByVal filterInfo As Object, _
            ByVal stmt As StmtGenerator, ByRef f As IFilter, ByVal selectOS As EntityUnion)

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
                If SelectList IsNot Nothing AndAlso SelectList.Count > 0 Then
                    Throw New NotSupportedException("Cannot select individual column in m2m query")
                End If

                Dim selectedType As Type = selectType
                Dim filteredType As Type = _m2mObject.GetType

                'Dim schema2 As IOrmObjectSchema = GetEntitySchema(filteredType)

                'column - select
                Dim selected_r As M2MRelationDesc = Nothing
                'column - filter
                Dim filtered_r As M2MRelationDesc = Nothing

                filtered_r = schema.GetM2MRelation(selectedType, filteredType, _m2mKey)
                Dim revKey As String = _m2mKey
                If selectedType Is filteredType Then
                    revKey = M2MRelationDesc.GetRevKey(_m2mKey)
                End If
                selected_r = schema.GetM2MRelation(filteredType, selectedType, revKey)
                Dim needReplace As RelationDesc = Nothing
                If selected_r Is Nothing Then
                    Throw New ObjectMappingException(String.Format("Type {0} has no relation to {1}", selectedType.Name, filteredType.Name))
                Else
                    If _rel.Relation Is Nothing Then
                        needReplace = selected_r
                    ElseIf _rel.Relation.Rel Is Nothing Then
                        needReplace = New RelationDesc(selected_r.Rel, _rel.Relation.Column, _rel.Relation.Key)
                    ElseIf _rel.Relation.Rel.GetRealType(schema) IsNot selectedType Then
                        Throw New QueryCmdException(String.Format("Relation type is {0}, selected type is {1}", _rel.Relation.Rel.GetRealType(schema), selectType), Me)
                    End If
                End If

                If needReplace IsNot Nothing Then
                    Dim newRel As Relation = Nothing
                    If GetType(M2MRelationDesc).IsAssignableFrom(needReplace.GetType) Then
                        newRel = New M2MRelation(_rel.Host, CType(needReplace, M2MRelationDesc))
                    Else
                        newRel = New Relation(_rel.Host, needReplace)
                    End If
                    _m2mObject._ReplaceRel(_rel, newRel)
                    _rel = newRel
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

                If AppendMain OrElse WithLoad(selectOS, schema) OrElse IsFTS Then
                    AppendMain = True
                    Dim jf As New JoinFilter(table, selected_r.Column, _
                        selectType, schema.GetPrimaryKeys(selectedType)(0).PropertyAlias, Criteria.FilterOperation.Equal)
                    Dim jn As New QueryJoin(table, JoinType.Join, jf)
                    _js.Add(jn)
                    If _from Is Nothing OrElse table.Equals(_from.Table) Then
                        _from = New FromClauseDef(selectOS)
                    End If
                    If WithLoad(selectOS, schema) Then
                        _sl.AddRange(schema.GetSortedFieldList(selectedType).ConvertAll(Function(c As EntityPropertyAttribute) ObjectMappingEngine.ConvertColumn2SelExp(c, selectOS)))
                    Else
                        GoTo l1
                    End If
                Else
                    _from = New FromClauseDef(table)
                    'Dim os As IOrmObjectSchemaBase = schema.GetEntitySchema(selectedType)
                    'os.GetFieldColumnMap()("ID")._columnName
l1:
                    Dim pk As EntityPropertyAttribute = schema.GetPrimaryKeys(selectType)(0)
                    Dim se As New SelectExpression(table, selected_r.Column, pk.PropertyAlias)
                    se.Attributes = Field2DbRelations.PK
                    _sl.Add(se)

                    If SelectTypes(0).First.Equals(selectOS) Then
                    Else
                        Throw New NotImplementedException
                    End If
                End If

                If SelectTypes.Count > 1 Then
                    For i As Integer = 1 To SelectTypes.Count - 1
                        AddTypeFields(schema, _sl, SelectTypes(i))
                    Next
                End If

                Dim tf As New TableFilter(table, filtered_r.Column, _
                    New Worm.Criteria.Values.ScalarValue(_m2mObject.Identifier), Criteria.FilterOperation.Equal)
                Dim con As Condition.ConditionConstructor = New Condition.ConditionConstructor
                con.AddFilter(f)
                con.AddFilter(tf)
                f = con.Condition

                _f = f
                Return
            End If
        End Sub
    End Class
End Namespace