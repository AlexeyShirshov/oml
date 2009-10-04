﻿Imports Worm.Query
Imports Worm.Criteria.Core
Imports System.Collections.Generic
Imports Worm.Expressions2

Namespace Entities.Meta

    Public Structure DBType
        Public Size As Integer
        Public Type As String
        Public Nullable As Boolean

        Public Sub New(ByVal type As String)
            Me.Type = type
        End Sub

        Public Sub New(ByVal type As String, ByVal size As Integer)
            Me.Type = type
            Me.Size = size
        End Sub

        Public Sub New(ByVal type As String, ByVal size As Integer, ByVal nullable As Boolean)
            Me.Type = type
            Me.Size = size
            Me.Nullable = nullable
        End Sub

        Public Sub New(ByVal type As String, ByVal nullable As Boolean)
            Me.Type = type
            Me.Nullable = nullable
        End Sub

        Public Function IsEmpty() As Boolean
            Return String.IsNullOrEmpty(Type)
        End Function
    End Structure

    Public Class MapField2Column
        Implements ICloneable

        Public ReadOnly _propertyAlias As String
        Public ReadOnly ColumnExpression As String
        Public ReadOnly DBType As DBType
        Public ReadOnly _newattributes As Field2DbRelations
        Private _tbl As SourceFragment
        Private _columnName As String

        Public Sub New(ByVal propertyAlias As String, ByVal columnExpression As String, ByVal tableName As SourceFragment)
            _propertyAlias = propertyAlias
            Me.ColumnExpression = columnExpression
            Table = tableName
            _newattributes = Field2DbRelations.None
        End Sub

        Public Sub New(ByVal propertyAlias As String, ByVal columnExpression As String, ByVal tableName As SourceFragment, _
            ByVal newAttributes As Field2DbRelations)
            _propertyAlias = propertyAlias
            Me.ColumnExpression = columnExpression
            Table = tableName
            _newattributes = newAttributes
        End Sub

        Public Sub New(ByVal propertyAlias As String, ByVal columnExpression As String, ByVal tableName As SourceFragment, _
            ByVal newAttributes As Field2DbRelations, ByVal dbType As DBType)
            MyClass.New(propertyAlias, columnExpression, tableName, newAttributes)
            Me.DBType = dbType
        End Sub

        Public Sub New(ByVal propertyAlias As String, ByVal columnExpression As String, ByVal tableName As SourceFragment, _
            ByVal newAttributes As Field2DbRelations, ByVal dbType As String)
            MyClass.New(propertyAlias, columnExpression, tableName, newAttributes)
            Me.DBType = New DBType(dbType)
        End Sub

        Public Sub New(ByVal propertyAlias As String, ByVal columnExpression As String, ByVal tableName As SourceFragment, _
            ByVal newAttributes As Field2DbRelations, ByVal dbType As String, ByVal size As Integer)
            MyClass.New(propertyAlias, columnExpression, tableName, newAttributes)
            Me.DBType = New DBType(dbType, size)
        End Sub

        Public Sub New(ByVal propertyAlias As String, ByVal columnExpression As String, ByVal tableName As SourceFragment, _
            ByVal newAttributes As Field2DbRelations, ByVal dbType As String, ByVal size As Integer, ByVal nullable As Boolean)
            MyClass.New(propertyAlias, columnExpression, tableName, newAttributes)
            Me.DBType = New DBType(dbType, size, nullable)
        End Sub

        Public Sub New(ByVal propertyAlias As String, ByVal columnExpression As String, ByVal tableName As SourceFragment, _
            ByVal newAttributes As Field2DbRelations, ByVal dbType As String, ByVal nullable As Boolean)
            MyClass.New(propertyAlias, columnExpression, tableName, newAttributes)
            Me.DBType = New DBType(dbType, nullable)
        End Sub

        Public Function GetAttributes(ByVal c As EntityPropertyAttribute) As Field2DbRelations
            If _newattributes = Field2DbRelations.None Then
                Return c.Behavior()
            Else
                Return _newattributes
            End If
        End Function

        Public Property Table() As SourceFragment
            Get
                Return _tbl
            End Get
            Protected Friend Set(ByVal value As SourceFragment)
                _tbl = value
            End Set
        End Property

        Public Property ColumnName() As String
            Get
                Return _columnName
            End Get
            Set(ByVal value As String)
                _columnName = value
            End Set
        End Property

        Public Function Clone() As Object Implements System.ICloneable.Clone
            Return New MapField2Column(Me._propertyAlias, ColumnExpression, Table, Me._newattributes, Me.DBType)
        End Function
    End Class

    Public Class RelationDesc
        Public ReadOnly Column As String
        Public ReadOnly Key As String
        Private _eu As EntityUnion

        Public Sub New(ByVal eu As EntityUnion, ByVal propertyAlias As String)
            Me.Column = propertyAlias
            _eu = eu
        End Sub

        Public Sub New(ByVal eu As EntityUnion, ByVal propertyAlias As String, ByVal key As String)
            Me.Column = propertyAlias
            Me.Key = key
            _eu = eu
        End Sub

        Public Property Entity() As EntityUnion
            Get
                Return _eu
            End Get
            Friend Set(ByVal value As EntityUnion)
                _eu = value
            End Set
        End Property

        Public ReadOnly Property EntityName() As String
            Get
                Return _eu.EntityName
            End Get
        End Property

        Public ReadOnly Property Type() As Type
            Get
                Return _eu.EntityType
            End Get
        End Property

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return Equals(TryCast(obj, RelationDesc))
        End Function

        Public Overloads Function Equals(ByVal obj As RelationDesc) As Boolean
            If obj Is Nothing Then
                Return False
            End If
            Return Object.Equals(_eu, obj._eu) AndAlso M2MRelationDesc.CompareKeys(Key, obj.Key)
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return _eu.GetHashCode Xor If(String.IsNullOrEmpty(Key), 0, Key.GetHashCode)
        End Function

        Class cls
            Private _ids As List(Of Object)
            Private _f As FieldReference

            Public Sub New(ByVal ids As List(Of Object))
                _ids = ids
            End Sub

            Public Sub OnModifyFilter(ByVal sender As RelationCmd, ByVal args As RelationCmd.ModifyFilter)
                'Dim tf As TableFilter = TryCast(args.RelFilter, TableFilter)
                'If tf IsNot Nothing Then
                '    _f = New FieldReference(tf.Template.Table, tf.Template.Column)
                'Else
                '    Dim ef As EntityFilter = TryCast(args.RelFilter, EntityFilter)
                '    _f = New FieldReference(ef.Template.ObjectSource, ef.Template.PropertyAlias)
                'End If
                'args.RelFilter = New CustomFilter("1", Criteria.FilterOperation.Equal, New Criteria.Values.LiteralValue("1"))
                'args.Modified = True
                'sender.SetBatchFilter(_ids, _f)
                Dim inv As New Criteria.Values.InValue(_ids)
                Dim tf As TableFilter = TryCast(args.RelFilter, TableFilter)
                If tf IsNot Nothing Then
                    Dim tt As New TableFilterTemplate(tf.Template.Table, tf.Template.Column, Criteria.FilterOperation.In)
                    args.RelFilter = New TableFilter(inv, tt)
                Else
                    Dim ef As EntityFilter = TryCast(args.RelFilter, EntityFilter)
                    Dim tt As New OrmFilterTemplate(ef.Template.ObjectSource, ef.Template.PropertyAlias, Criteria.FilterOperation.In)
                    args.RelFilter = New EntityFilter(inv, tt)
                End If
                args.Modified = True
                sender.OptimizeInFilter(args.RelFilter)
            End Sub

            Private ReadOnly Property BatchFilter() As Pair(Of List(Of Object), FieldReference)
                Get
                    Return New Pair(Of List(Of Object), FieldReference)(_ids, _f)
                End Get
            End Property
        End Class

        Public Overridable Function Load(Of T As IKeyEntity, ReturnType As _IKeyEntity)(ByVal objs As IList(Of T), ByVal loadWithObjects As Boolean) As ReadOnlyList(Of ReturnType)
            Return Load(Of T, ReturnType)(objs, 0, objs.Count, loadWithObjects)
        End Function

        Public Overridable Function Load(Of T As IKeyEntity, ReturnType As _IKeyEntity)(ByVal objs As IList(Of T), _
                                    ByVal pager As IPager, ByVal loadWithObjects As Boolean) As ReadOnlyList(Of ReturnType)
            If pager Is Nothing Then
                Return Load(Of T, ReturnType)(objs, loadWithObjects)
            Else
                Return Load(Of T, ReturnType)(objs, pager.GetCurrentPageOffset, Math.Min(objs.Count - pager.GetCurrentPageOffset, pager.GetPageSize), loadWithObjects)
            End If
        End Function

        Public Overridable Function Load(Of T As IKeyEntity, ReturnType As _IKeyEntity)(ByVal objs As IList(Of T), _
                                    ByVal start As Integer, ByVal length As Integer, ByVal loadWithObjects As Boolean) As ReadOnlyList(Of ReturnType)
            Dim lookups As New Dictionary(Of IKeyEntity, IList)
            Dim newc As New List(Of IKeyEntity)
            Dim hasInCache As New Dictionary(Of IKeyEntity, Object)
            Dim rcmd As RelationCmd = Nothing
            Dim rt As Type = Nothing
            Dim mpe As ObjectMappingEngine = Nothing

            For i As Integer = start To start + length - 1
                Dim o As IKeyEntity = objs(i)
                If rt Is Nothing Then
                    rt = o.GetType
                End If
                If o.ObjectState <> ObjectState.Created Then
                    Dim ncmd As RelationCmd = CreateCmd(o)

                    If ncmd.IsInCache Then
                        lookups.Add(o, ncmd.ToList)
                        hasInCache.Add(o, Nothing)
                    Else
                        newc.Add(o)
                    End If
                    If rcmd Is Nothing Then
                        rcmd = ncmd
                        Using mgr As OrmManager = rcmd.CreateManager.CreateManager
                            mpe = mgr.MappingEngine
                        End Using
                        Dim retType As Type = rcmd.RelationDesc.Entity.GetRealType(mpe)
                        If Not GetType(ReturnType).IsAssignableFrom(retType) Then
                            If GetType(T).IsAssignableFrom(retType) Then
                                Dim re As ReadOnlyList(Of T) = TryCast(objs, ReadOnlyList(Of T))
                                If re Is Nothing Then
                                    re = New ReadOnlyList(Of T)(objs)
                                End If
                                Dim rv As ReadOnlyList(Of ReturnType) = re.SelectEntity(Of ReturnType)(start, length, Column)
                                rv.LoadObjects()
                                Return rv
                            Else
                                Throw New ArgumentException("Generic params is not correspond to QueryCmd")
                            End If
                        End If
                    End If
                End If
            Next

            If rcmd Is Nothing Then
                Return New ReadOnlyList(Of ReturnType)
            Else
                rcmd = CType(rcmd.Clone, RelationCmd)
            End If

            Dim ids As New List(Of Object)
            For Each o As IKeyEntity In newc
                ids.Add(o.Identifier)
            Next

            Dim c As New cls(ids)
            AddHandler rcmd.OnModifyFilter, AddressOf c.OnModifyFilter
            rcmd.RenewMark()

            If rcmd.IsM2M Then
                If loadWithObjects Then
                    rcmd.WithLoad(True)
                End If

                rcmd.QueryWithHost = True

                Dim r As ReadonlyMatrix = rcmd.ToMatrix

                For Each row As ObjectModel.ReadOnlyCollection(Of _IEntity) In r
                    Dim key As IKeyEntity = CType(row(1), IKeyEntity)
                    Dim val As ReturnType = CType(row(0), ReturnType)

                    Dim ll As IList = Nothing
                    If Not lookups.TryGetValue(key, ll) Then
                        ll = New ReadOnlyList(Of ReturnType)
                        lookups.Add(key, ll)
                    End If
                    CType(ll, IListEdit).Add(val)
                Next
            Else
                Dim op As New ObjectProperty(rcmd.RelationDesc.Entity, rcmd.RelationDesc.Column)
                Dim rtt As Type = Nothing
                Dim oschema As IEntitySchema = Nothing

                rtt = op.Entity.GetRealType(mpe)
                oschema = mpe.GetEntitySchema(rtt)

                If loadWithObjects Then
                    rcmd.WithLoad(True)
                Else
                    Dim se As List(Of SelectExpression) = mpe.GetPrimaryKeys(rtt, oschema).ConvertAll(Function(clm As EntityPropertyAttribute) ObjectMappingEngine.ConvertColumn2SelExp(clm, rtt))
                    se.Add(FCtor.prop(op))
                    rcmd.Select(se.ToArray)
                End If

                Dim r As ReadOnlyList(Of ReturnType) = rcmd.ToOrmListDyn(Of ReturnType)()

                For Each o As ReturnType In r
                    Dim key As IKeyEntity = CType(mpe.GetPropertyValue(o, op.PropertyAlias, oschema), IKeyEntity)
                    Dim ll As IList = Nothing
                    If Not lookups.TryGetValue(key, ll) Then
                        ll = New ReadOnlyList(Of ReturnType)
                        lookups.Add(key, ll)
                    End If
                    CType(ll, IListEdit).Add(o)
                Next
            End If

            Dim l As New ReadOnlyList(Of ReturnType)
            For i As Integer = start To start + length - 1
                Dim o As IKeyEntity = objs(i)
                Dim v As IList = Nothing
                If lookups.TryGetValue(o, v) Then
                    For Each oo As IEntity In v
                        CType(l, IListEdit).Add(oo)
                    Next
                Else
                    v = New ReadOnlyList(Of ReturnType)
                End If
                Dim ncmd As RelationCmd = CreateCmd(o)
                If Not hasInCache.ContainsKey(o) Then
                    ncmd.SetCache(v)
                End If
            Next

            Return l
        End Function

        Public Overridable Function Load(Of T As IKeyEntity, ReturnType As _IKeyEntity)(ByVal objs As IEnumerable(Of T), _
            ByVal loadWithObjects As Boolean) As ReadOnlyList(Of ReturnType)
            Dim lookups As New Dictionary(Of IKeyEntity, IList)
            Dim newc As New List(Of IKeyEntity)
            Dim hasInCache As New Dictionary(Of IKeyEntity, Object)
            Dim rcmd As RelationCmd = Nothing
            Dim rt As Type = Nothing
            Dim mpe As ObjectMappingEngine = Nothing

            For Each o As IKeyEntity In objs

                If rt Is Nothing Then
                    rt = o.GetType
                End If
                If o.ObjectState <> ObjectState.Created Then
                    Dim ncmd As RelationCmd = CreateCmd(o)

                    If ncmd.IsInCache Then
                        lookups.Add(o, ncmd.ToList)
                        hasInCache.Add(o, Nothing)
                    Else
                        newc.Add(o)
                    End If
                    If rcmd Is Nothing Then
                        rcmd = ncmd
                        Using mgr As OrmManager = rcmd.CreateManager.CreateManager
                            mpe = mgr.MappingEngine
                        End Using
                        Dim retType As Type = rcmd.RelationDesc.Entity.GetRealType(mpe)
                        If Not GetType(ReturnType).IsAssignableFrom(retType) Then
                            If GetType(T).IsAssignableFrom(retType) Then
                                Dim re As ReadOnlyList(Of T) = TryCast(objs, ReadOnlyList(Of T))
                                If re Is Nothing Then
                                    re = New ReadOnlyList(Of T)(objs)
                                End If
                                Dim rv As ReadOnlyList(Of ReturnType) = re.SelectEntity(Of ReturnType)(Column)
                                rv.LoadObjects()
                                Return rv
                            Else
                                Throw New ArgumentException("Generic params is not correspond to QueryCmd")
                            End If
                        End If
                    End If
                End If
            Next

            If rcmd Is Nothing Then
                Return New ReadOnlyList(Of ReturnType)
            Else
                rcmd = CType(rcmd.Clone, RelationCmd)
            End If

            Dim ids As New List(Of Object)
            For Each o As IKeyEntity In newc
                ids.Add(o.Identifier)
            Next

            Dim c As New cls(ids)
            AddHandler rcmd.OnModifyFilter, AddressOf c.OnModifyFilter
            rcmd.RenewMark()

            If rcmd.IsM2M Then
                If loadWithObjects Then
                    rcmd.WithLoad(True)
                End If

                rcmd.QueryWithHost = True

                Dim r As ReadonlyMatrix = rcmd.ToMatrix

                For Each row As ObjectModel.ReadOnlyCollection(Of _IEntity) In r
                    Dim key As IKeyEntity = CType(row(1), IKeyEntity)
                    Dim val As ReturnType = CType(row(0), ReturnType)

                    Dim ll As IList = Nothing
                    If Not lookups.TryGetValue(key, ll) Then
                        ll = New ReadOnlyList(Of ReturnType)
                        lookups.Add(key, ll)
                    End If
                    CType(ll, IListEdit).Add(val)
                Next
            Else
                Dim op As New ObjectProperty(rcmd.RelationDesc.Entity, rcmd.RelationDesc.Column)
                Dim rtt As Type = Nothing
                Dim oschema As IEntitySchema = Nothing

                rtt = op.Entity.GetRealType(mpe)
                oschema = mpe.GetEntitySchema(rtt)

                If loadWithObjects Then
                    rcmd.WithLoad(True)
                Else
                    Dim se As List(Of SelectExpression) = mpe.GetPrimaryKeys(rtt, oschema).ConvertAll(Function(clm As EntityPropertyAttribute) ObjectMappingEngine.ConvertColumn2SelExp(clm, rtt))
                    se.Add(FCtor.prop(op))
                    rcmd.Select(se.ToArray)
                End If

                Dim r As ReadOnlyList(Of ReturnType) = rcmd.ToOrmListDyn(Of ReturnType)()

                For Each o As ReturnType In r
                    Dim key As IKeyEntity = CType(mpe.GetPropertyValue(o, op.PropertyAlias, oschema), IKeyEntity)
                    Dim ll As IList = Nothing
                    If Not lookups.TryGetValue(key, ll) Then
                        ll = New ReadOnlyList(Of ReturnType)
                        lookups.Add(key, ll)
                    End If
                    CType(ll, IListEdit).Add(o)
                Next
            End If

            Dim l As New ReadOnlyList(Of ReturnType)

            For Each o As IKeyEntity In objs
                Dim v As IList = Nothing
                If lookups.TryGetValue(o, v) Then
                    For Each oo As IEntity In v
                        CType(l, IListEdit).Add(oo)
                    Next
                Else
                    v = New ReadOnlyList(Of ReturnType)
                End If
                Dim ncmd As RelationCmd = CreateCmd(o)
                If Not hasInCache.ContainsKey(o) Then
                    ncmd.SetCache(v)
                End If
            Next

            Return l
        End Function

        Public Overridable Function Load(Of T As IKeyEntity, ReturnType As _IKeyEntity)(ByVal objs As IEnumerable(Of T), _
            ByVal loadWithObjects As Boolean, ByVal mgr As OrmManager) As ReadOnlyList(Of ReturnType)
            Dim lookups As New Dictionary(Of IKeyEntity, IList)
            Dim newc As New List(Of IKeyEntity)
            Dim hasInCache As New Dictionary(Of IKeyEntity, Object)
            Dim rcmd As RelationCmd = Nothing
            Dim rt As Type = Nothing

            For Each o As IKeyEntity In objs

                If rt Is Nothing Then
                    rt = o.GetType
                End If
                If o.ObjectState <> ObjectState.Created Then
                    Dim ncmd As RelationCmd = CreateCmd(o)

                    If ncmd.IsInCache(mgr) Then
                        lookups.Add(o, ncmd.ToList(mgr))
                        hasInCache.Add(o, Nothing)
                    Else
                        newc.Add(o)
                    End If
                    If rcmd Is Nothing Then
                        rcmd = ncmd
                        Dim retType As Type = rcmd.RelationDesc.Entity.GetRealType(mgr.MappingEngine)
                        If Not GetType(ReturnType).IsAssignableFrom(retType) Then
                            If GetType(T).IsAssignableFrom(retType) Then
                                Dim re As ReadOnlyList(Of T) = TryCast(objs, ReadOnlyList(Of T))
                                If re Is Nothing Then
                                    re = New ReadOnlyList(Of T)(objs)
                                End If
                                Dim rv As ReadOnlyList(Of ReturnType) = re.SelectEntity(Of ReturnType)(Column)
                                rv.LoadObjects(mgr)
                                Return rv
                            Else
                                Throw New ArgumentException("Generic params is not correspond to QueryCmd")
                            End If
                        End If
                    End If
                End If
            Next

            If rcmd Is Nothing Then
                Return New ReadOnlyList(Of ReturnType)
            Else
                rcmd = CType(rcmd.Clone, RelationCmd)
            End If

            Dim ids As New List(Of Object)
            For Each o As IKeyEntity In newc
                ids.Add(o.Identifier)
            Next

            Dim c As New cls(ids)
            AddHandler rcmd.OnModifyFilter, AddressOf c.OnModifyFilter
            rcmd.RenewMark()

            If rcmd.IsM2M Then
                If loadWithObjects Then
                    rcmd.WithLoad(True)
                End If

                rcmd.QueryWithHost = True

                Dim r As ReadonlyMatrix = rcmd.ToMatrix(mgr)

                For Each row As ObjectModel.ReadOnlyCollection(Of _IEntity) In r
                    Dim key As IKeyEntity = CType(row(1), IKeyEntity)
                    Dim val As ReturnType = CType(row(0), ReturnType)

                    Dim ll As IList = Nothing
                    If Not lookups.TryGetValue(key, ll) Then
                        ll = New ReadOnlyList(Of ReturnType)
                        lookups.Add(key, ll)
                    End If
                    CType(ll, IListEdit).Add(val)
                Next
            Else
                Dim op As New ObjectProperty(rcmd.RelationDesc.Entity, rcmd.RelationDesc.Column)
                Dim rtt As Type = Nothing
                Dim oschema As IEntitySchema = Nothing

                rtt = op.Entity.GetRealType(mgr.MappingEngine)
                oschema = mgr.MappingEngine.GetEntitySchema(rtt)

                If loadWithObjects Then
                    rcmd.WithLoad(True)
                Else
                    Dim se As List(Of SelectExpression) = mgr.MappingEngine.GetPrimaryKeys(rtt, oschema).ConvertAll(Function(clm As EntityPropertyAttribute) ObjectMappingEngine.ConvertColumn2SelExp(clm, rtt))
                    se.Add(FCtor.prop(op))
                    rcmd.Select(se.ToArray)
                End If

                Dim r As ReadOnlyList(Of ReturnType) = rcmd.ToOrmListDyn(Of ReturnType)(mgr)

                For Each o As ReturnType In r
                    Dim key As IKeyEntity = CType(mgr.MappingEngine.GetPropertyValue(o, op.PropertyAlias, oschema), IKeyEntity)
                    Dim ll As IList = Nothing
                    If Not lookups.TryGetValue(key, ll) Then
                        ll = New ReadOnlyList(Of ReturnType)
                        lookups.Add(key, ll)
                    End If
                    CType(ll, IListEdit).Add(o)
                Next
            End If

            Dim l As New ReadOnlyList(Of ReturnType)

            For Each o As IKeyEntity In objs
                Dim v As IList = Nothing
                If lookups.TryGetValue(o, v) Then
                    For Each oo As IEntity In v
                        CType(l, IListEdit).Add(oo)
                    Next
                Else
                    v = New ReadOnlyList(Of ReturnType)
                End If
                Dim ncmd As RelationCmd = CreateCmd(o)
                If Not hasInCache.ContainsKey(o) Then
                    ncmd.SetCache(mgr, v)
                End If
            Next

            Return l
        End Function

        Public Overridable Function CreateCmd(ByVal o As IKeyEntity) As RelationCmd
            Return o.GetCmd(Me)
        End Function

    End Class

    Public Class M2MRelationDesc
        Inherits RelationDesc
        Public ReadOnly Table As SourceFragment
        Public ReadOnly DeleteCascade As Boolean
        Public ReadOnly Mapping As System.Data.Common.DataTableMapping
        Public ReadOnly ConnectedType As Type

        Private _const() As IFilter

        Public Const DirKey As String = "xxx%direct$"
        Public Const ReversePrefix As String = "$rev$"
        Public Const RevKey As String = ReversePrefix & DirKey

        Public Sub New(ByVal type As Type)
            MyBase.New(New EntityUnion(type), Nothing, Nothing)
        End Sub

        Public Sub New(ByVal type As Type, ByVal key As String)
            MyBase.New(New EntityUnion(type), Nothing, key)
        End Sub

        Public Sub New(ByVal entityName As String)
            MyBase.New(New EntityUnion(entityName), Nothing, Nothing)
        End Sub

        Public Sub New(ByVal entityName As String, ByVal key As String)
            MyBase.New(New EntityUnion(entityName), Nothing, key)
        End Sub

        Public Sub New(ByVal eu As EntityUnion)
            MyBase.New(eu, Nothing, Nothing)
        End Sub

        Public Sub New(ByVal eu As EntityUnion, ByVal key As String)
            MyBase.New(eu, Nothing, key)
        End Sub

        Public Sub New(ByVal type As Type, ByVal column As String, ByVal key As String)
            MyBase.New(New EntityUnion(type), column, key)
        End Sub

        Public Sub New(ByVal entityName As String, ByVal column As String, ByVal key As String)
            MyBase.New(New EntityUnion(entityName), column, key)
        End Sub

        Public Sub New(ByVal eu As EntityUnion, ByVal column As String, ByVal key As String)
            MyBase.New(eu, column, key)
        End Sub

#Region " Type ctors "

        Public Sub New(ByVal type As Type, ByVal table As SourceFragment, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping, ByVal key As String)
            MyBase.New(New EntityUnion(type), column, key)
            Me.Table = table
            Me.DeleteCascade = delete
            Me.Mapping = mapping
        End Sub

        Public Sub New(ByVal type As Type, ByVal table As SourceFragment, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping, ByVal key As String, ByVal connectedType As Type)
            MyClass.New(type, table, column, delete, mapping, key)
            Me.ConnectedType = connectedType
        End Sub

        Public Sub New(ByVal type As Type, ByVal table As SourceFragment, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping, _
            ByVal key As String, ByVal connectedType As Type, ByVal constFields() As IFilter)
            MyClass.New(type, table, column, delete, mapping, key, connectedType)
            _const = constFields
        End Sub

        Public Sub New(ByVal type As Type, ByVal table As SourceFragment, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping, ByVal connectedType As Type)
            MyClass.New(type, table, column, delete, mapping)
            Me.ConnectedType = connectedType
        End Sub

        Public Sub New(ByVal type As Type, ByVal table As SourceFragment, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping)
            MyClass.New(type, table, column, delete, mapping, DirKey)
        End Sub

#End Region

#Region " Entityname ctors "

        Public Sub New(ByVal entityName As String, ByVal table As SourceFragment, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping, ByVal key As String)
            MyBase.New(New EntityUnion(entityName), column, key)
            Me.Table = table
            Me.DeleteCascade = delete
            Me.Mapping = mapping
        End Sub

        Public Sub New(ByVal entityName As String, ByVal table As SourceFragment, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping, ByVal key As String, ByVal connectedType As Type)
            MyClass.New(entityName, table, column, delete, mapping, key)
            Me.ConnectedType = connectedType
        End Sub

        Public Sub New(ByVal entityName As String, ByVal table As SourceFragment, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping, _
            ByVal key As String, ByVal connectedType As Type, ByVal constFields() As IFilter)
            MyClass.New(entityName, table, column, delete, mapping, key, connectedType)
            _const = constFields
        End Sub

        Public Sub New(ByVal entityName As String, ByVal table As SourceFragment, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping)
            MyClass.New(entityName, table, column, delete, mapping, DirKey)
        End Sub

        Public Sub New(ByVal entityName As String, ByVal table As SourceFragment, ByVal column As String, _
            ByVal delete As Boolean, ByVal mapping As System.Data.Common.DataTableMapping, ByVal connectedType As Type)
            MyClass.New(entityName, table, column, delete, mapping)
            Me.ConnectedType = connectedType
        End Sub

#End Region

        Public ReadOnly Property Constants() As IFilter()
            Get
                Return _const
            End Get
        End Property

        Public ReadOnly Property non_direct() As Boolean
            Get
                Return Not String.IsNullOrEmpty(Key) AndAlso Key.StartsWith(ReversePrefix)
            End Get
        End Property

        Public Shared Function IsDirect(ByVal key As String) As Boolean
            If String.IsNullOrEmpty(key) OrElse Not key.StartsWith(ReversePrefix) Then
                Return True
            Else
                Return False
            End If
        End Function

        Public Shared Function GetKey(ByVal direct As Boolean) As String
            If direct Then
                Return DirKey
            Else
                Return RevKey
            End If
        End Function

        Public Shared Function GetRevKey(ByVal key As String) As String
            If Not String.IsNullOrEmpty(key) Then
                If key.StartsWith(ReversePrefix) Then
                    Return key.Remove(0, ReversePrefix.Length)
                Else
                    Return ReversePrefix & key
                End If
            End If
            Return key
        End Function

        Public Shared Function CompareKeys(ByVal key1 As String, ByVal key2 As String) As Boolean
            If key1 Is Nothing AndAlso key2 Is Nothing Then
                Return True
            Else
                If (key1 Is Nothing AndAlso key2 = DirKey) OrElse _
                    (key2 Is Nothing AndAlso key1 = DirKey) Then
                    Return True
                Else
                    Return String.Equals(key1, key2)
                End If
            End If
        End Function
    End Class

    Public Class RelationDescEx
        Public Rel As RelationDesc
        Public HostEntity As EntityUnion

        Public Sub New(ByVal eu As EntityUnion, ByVal r As RelationDesc)
            Rel = r
            HostEntity = eu
        End Sub

        Public ReadOnly Property M2MRel() As M2MRelationDesc
            Get
                Return CType(Rel, M2MRelationDesc)
            End Get
        End Property

        Public ReadOnly Property Key() As String
            Get
                Return Rel.Key
            End Get
        End Property

        Public ReadOnly Property Entity() As EntityUnion
            Get
                Return Rel.Entity
            End Get
        End Property

        Public ReadOnly Property EntityName() As String
            Get
                Return Rel.EntityName
            End Get
        End Property

        Public ReadOnly Property Type() As Type
            Get
                Return Rel.Type
            End Get
        End Property

        Public Function Load(Of T As IKeyEntity, ReturnType As _IKeyEntity)(ByVal objs As IList(Of T), ByVal loadWithObjects As Boolean) As ReadOnlyList(Of ReturnType)
            Return Rel.Load(Of T, ReturnType)(objs, loadWithObjects)
        End Function

        Public Function Load(Of T As IKeyEntity, ReturnType As _IKeyEntity)(ByVal objs As IList(Of T), _
                                    ByVal pager As IPager, ByVal loadWithObjects As Boolean) As ReadOnlyList(Of ReturnType)
            Return Rel.Load(Of T, ReturnType)(objs, pager, loadWithObjects)
        End Function

        Public Function Load(Of T As IKeyEntity, ReturnType As _IKeyEntity)(ByVal objs As IList(Of T), _
                                    ByVal start As Integer, ByVal length As Integer, ByVal loadWithObjects As Boolean) As ReadOnlyList(Of ReturnType)
            Return Rel.Load(Of T, ReturnType)(objs, start, length, loadWithObjects)
        End Function

        Public Function Load(Of T As IKeyEntity, ReturnType As _IKeyEntity)(ByVal objs As IEnumerable(Of T), ByVal loadWithObjects As Boolean) As ReadOnlyList(Of ReturnType)
            Return Rel.Load(Of T, ReturnType)(objs, loadWithObjects)
        End Function

        Public Shared Widening Operator CType(ByVal r As RelationDescEx) As RelationDesc
            Return r.Rel
        End Operator

        Public Shared Widening Operator CType(ByVal r As RelationDescEx) As M2MRelationDesc
            Return r.M2MRel
        End Operator
    End Class

    ''' <summary>
    ''' Индексированая по полю <see cref="MapField2Column._propertyAlias"/> колекция объектов типа <see cref="MapField2Column"/>
    ''' </summary>
    ''' <remarks>
    ''' Наследник абстрактного класс <see cref="Collections.IndexedCollection(Of string, MapField2Column)"/>, реализующий метод <see cref="Collections.IndexedCollection(Of string, MapField2Column).GetKeyForItem" />
    ''' </remarks>
    Public Class OrmObjectIndex
        Inherits Collections.IndexedCollection(Of String, MapField2Column)

        ''' <summary>
        ''' Возвращает ключ коллекции MapField2Column
        ''' </summary>
        ''' <param name="item">Элемент коллекции</param>
        ''' <returns>Возвращает <see cref="MapField2Column._propertyAlias"/></returns>
        ''' <remarks>Используется при индексации коллекции</remarks>
        Protected Overrides Function GetKeyForItem(ByVal item As MapField2Column) As String
            Return item._propertyAlias
        End Function
    End Class
End Namespace