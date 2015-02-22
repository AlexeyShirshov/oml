﻿Imports comp = System.ComponentModel
Imports System.Collections.Generic
Imports System.Collections.Specialized
Imports Worm.Criteria.Core
Imports Worm.Entities
Imports Worm.Entities.Meta
Imports Worm.Expressions2
Imports Worm.Query.Sorting
Imports Worm.Criteria.Joins
Imports Worm.Query

Friend Interface IListEdit
    Inherits IReadOnlyList, INotifyCollectionChanged, comp.INotifyPropertyChanged
    Overloads Sub Add(ByVal o As Entities.IEntity)
    Overloads Sub Remove(ByVal o As Entities.IEntity)
    Overloads Sub Insert(ByVal pos As Integer, ByVal o As Entities.IEntity)
    ReadOnly Property List() As IList
End Interface

Public Interface ILoadableList
    Inherits IList
    Sub LoadObjects()
    Sub LoadObjects(ByVal start As Integer, ByVal length As Integer)
End Interface

Public Interface IReadOnlyList
    Inherits IList, ICloneable
    Function CloneEmpty() As IReadOnlyList
    ReadOnly Property RealType() As Type
End Interface

<Serializable()> _
Public Class ReadOnlyList(Of T As {Entities.ISinglePKEntity})
    Inherits ReadOnlyEntityList(Of T)

    Private Shared _empty As New ReadOnlyList(Of T)

    Public Sub New(ByVal realType As Type)
        MyBase.new(realType)
    End Sub

    Public Sub New()
        MyBase.new()
    End Sub

    Public Sub New(ByVal realType As Type, ByVal col As IEnumerable(Of T))
        MyBase.New(realType, col)
    End Sub

    Public Sub New(ByVal col As IEnumerable(Of T))
        MyBase.New(col)
    End Sub

    Public Sub New(ByVal realType As Type, ByVal list As List(Of T))
        MyBase.New(realType, list)
    End Sub

    Public Sub New(ByVal realType As Type, ByVal list As ReadOnlyList(Of T))
        MyBase.New(realType, list)
    End Sub

    Public Shared ReadOnly Property Empty() As ReadOnlyList(Of T)
        Get
            Return _empty
        End Get
    End Property

    Public Overrides Function Distinct() As ReadOnlyEntityList(Of T)
        Return DistinctEntity()
    End Function

    Public Function DistinctEntity() As ReadOnlyList(Of T)
        Dim l As New Dictionary(Of T, T)
        For Each o As T In Me
            If Not l.ContainsKey(o) Then
                l.Add(o, o)
            End If
        Next
        Return New ReadOnlyList(Of T)(l.Keys)
    End Function

    Public Function LoadChildren(Of ReturnType As Entities._ISinglePKEntity)(ByVal rd As RelationDesc, ByVal loadWithObjects As Boolean) As ReadOnlyList(Of ReturnType)
        Return rd.Load(Of T, ReturnType)(Me, loadWithObjects)
    End Function

    Public Function LoadChildren(Of ReturnType As Entities._ISinglePKEntity)(ByVal rd As RelationDesc, ByVal loadWithObjects As Boolean, ByVal mgr As OrmManager) As ReadOnlyList(Of ReturnType)
        Return rd.Load(Of T, ReturnType)(Me, loadWithObjects, mgr)
    End Function

#If OLDM2M Then
    Public Function LoadChilds(Of ChildType As {New, Entities.IKeyEntity})() As ReadOnlyList(Of ChildType)
        If _l.Count > 0 Then
            Dim o As T = _l(0)
            Using mc As IGetManager = o.GetMgr()
                Dim fs As ICollection(Of String) = mc.Manager.MappingEngine.GetPropertyAliasByType(GetType(ChildType), GetType(T), Nothing)
                If fs.Count <> 1 Then
                    Throw New OrmManagerException("You must specify field")
                End If
                For Each f As String In fs
                    Return mc.Manager.LoadObjects(Of ChildType)(f, Nothing, Me)
                Next
            End Using
        End If
        Return New ReadOnlyList(Of ChildType)
    End Function

    Public Function LoadChilds(Of ChildType As {New, Entities.IKeyEntity})(ByVal childField As String) As ReadOnlyList(Of ChildType)
        If _l.Count > 0 Then
            Dim o As T = _l(0)
            Using mc As IGetManager = o.GetMgr()
                Return mc.Manager.LoadObjects(Of ChildType)(childField, Nothing, Me)
            End Using
        End If
        Return New ReadOnlyList(Of ChildType)
    End Function
#End If

    Public Overloads Function GetRange(ByVal index As Integer, ByVal count As Integer) As ReadOnlyList(Of T)
        If _l.Count > 0 Then
            Dim lst As List(Of T) = _l.GetRange(index, count)
            Dim ro As New ReadOnlyList(Of T)(lst)
            ro._rt = _rt
            Return ro
        Else
            Return Me
        End If
    End Function

    Friend Overrides Function CloneEmpty() As IReadOnlyList
        Return New ReadOnlyList(Of T)(_rt)
    End Function

    Public Overrides Function Clone() As Object
        Dim l As New List(Of T)
        l.AddRange(_l)
        Return New ReadOnlyList(Of T)(_rt, l)
    End Function

End Class

<Serializable()> _
Public Class ReadOnlyEntityList(Of T As Entities.ICachedEntity)
    Inherits ReadOnlyObjectList(Of T)
    Implements ILoadableList

    Protected _rt As Type

    Public Sub New()
        MyBase.new()
        _rt = GetType(T)
    End Sub

    Public Sub New(ByVal t As Type)
        MyBase.new()
        _rt = t
    End Sub

    Public Sub New(ByVal t As Type, ByVal col As IEnumerable(Of T))
        MyBase.New(col)
        _rt = t
    End Sub

    Public Sub New(ByVal col As IEnumerable(Of T))
        MyBase.New(col)
        _rt = GetType(T)
    End Sub

    Public Sub New(ByVal t As Type, ByVal list As List(Of T))
        MyBase.New(list)
        _rt = t
    End Sub

    Public Sub New(ByVal t As Type, ByVal list As ReadOnlyEntityList(Of T))
        MyBase.New(list)
        _rt = t
    End Sub

    Public Overridable Function Distinct() As ReadOnlyEntityList(Of T)
        Dim l As New Dictionary(Of T, T)
        For Each o As T In Me
            If Not l.ContainsKey(o) Then
                l.Add(o, o)
            End If
        Next
        Return New ReadOnlyEntityList(Of T)(l.Keys)
    End Function

    Public Overridable Overloads Function LoadObjects() As ReadOnlyEntityList(Of T)
        Return LoadObjects(0, _l.Count)
    End Function

    Public Overloads Function LoadObjects(ByVal mgr As OrmManager) As ReadOnlyEntityList(Of T)
        Return Query.QueryCmd.LoadObjects(Me, True, mgr)
    End Function

    Public Overloads Function LoadObjects(ByVal start As Integer, ByVal length As Integer, ByVal mgr As OrmManager) As ReadOnlyEntityList(Of T)
        Return Query.QueryCmd.LoadObjects(Me, start, length, True, mgr)
    End Function

    Private Function GetCMgr() As ICreateManager
        For Each o As T In _l
            If o.CreateManager IsNot Nothing Then
                Return o.CreateManager
            End If
        Next
        Return Nothing
    End Function

    Public Overridable Overloads Function LoadObjects(ByVal start As Integer, ByVal length As Integer) As ReadOnlyEntityList(Of T)
        If _l.Count > 0 Then
            Dim cmgr As ICreateManager = GetCMgr()

            If cmgr IsNot Nothing Then
                Dim cmd As New Query.QueryCmd(cmgr)
                Return cmd.SelectEntity(_rt, True).LoadObjects(Me, start, length)
            Else
                Return LoadObjects(start, length, OrmManager.CurrentManager)
            End If
        Else
            Return Me
        End If
    End Function

    Public Overridable Overloads Function LoadObjects(ByVal start As Integer, ByVal length As Integer, ByVal ParamArray properties2Load() As String) As ReadOnlyEntityList(Of T)
        If _l.Count > 0 Then
            Dim cmgr As ICreateManager = GetCMgr()

            Dim f As New Query.FCtor.Int
            For Each s As String In properties2Load
                f = f.prop(_rt, s)
            Next

            If cmgr IsNot Nothing Then
                Dim cmd As New Query.QueryCmd(cmgr)
                Return cmd.Select(f).From(_rt).LoadObjects(Me, start, length)
            Else
                Return Query.QueryCmd.LoadObjects(Me, start, length, f, OrmManager.CurrentManager)
            End If
        Else
            Return Me
        End If
    End Function

    Public Overridable Overloads Function LoadObjects(ByVal start As Integer, ByVal length As Integer, ByVal properties2Load As ObjectModel.ReadOnlyCollection(Of SelectExpression)) As ReadOnlyEntityList(Of T)
        If _l.Count > 0 Then
            Dim cmgr As ICreateManager = GetCMgr()

            If cmgr IsNot Nothing Then
                Dim cmd As New Query.QueryCmd(cmgr)
                'Dim frm As EntityUnion = _rt
                'For Each se As SelectExpression In properties2Load

                'Next
                Return cmd.Select(properties2Load).LoadObjects(Me, start, length)
            Else
                Return Query.QueryCmd.LoadObjects(Me, start, length, properties2Load, OrmManager.CurrentManager)
            End If
        Else
            Return Me
        End If
    End Function

    Private Sub _LoadObjects(ByVal start As Integer, ByVal length As Integer) Implements ILoadableList.LoadObjects
        LoadObjects(start, length)
    End Sub

    Private Sub _LoadObjects() Implements ILoadableList.LoadObjects
        LoadObjects()
    End Sub

    Public Sub LoadParentEntities(ByVal ParamArray parentPropertyAliases() As String)
        LoadParentEntities(0, Me.Count, parentPropertyAliases)
    End Sub

    Public Sub LoadParentEntities(ByVal start As Integer, ByVal length As Integer, ByVal ParamArray parentPropertyAliases() As String)
        Dim prop_objs(parentPropertyAliases.Length - 1) As IListEdit
        Dim mpe As ObjectMappingEngine = Nothing
        Dim oschema As IEntitySchema = Nothing
        For i As Integer = 0 To Me.Count - 1
            If start <= i AndAlso start + length > i Then
                Dim o As T = Me(i)
                If mpe Is Nothing Then
                    mpe = o.GetMappingEngine
                    If mpe Is Nothing AndAlso OrmManager.CurrentManager IsNot Nothing Then
                        mpe = OrmManager.CurrentManager.MappingEngine
                    End If
                    oschema = mpe.GetEntitySchema(o.GetType)
                End If
                For j As Integer = 0 To parentPropertyAliases.Length - 1
                    Dim propValue As Object = ObjectMappingEngine.GetPropertyValue(o, parentPropertyAliases(j), oschema)
                    Dim obj As IEntity = TryCast(propValue, IEntity)
                    If obj Is Nothing AndAlso propValue IsNot Nothing Then
                        Throw New ArgumentException("Property " & parentPropertyAliases(j) & " is not entity")
                    End If
                    If obj IsNot Nothing Then
                        If prop_objs(j) Is Nothing Then
                            prop_objs(j) = OrmManager._CreateReadOnlyList(obj.GetType)
                        End If
                        prop_objs(j).Add(obj)
                    End If
                Next
            End If
        Next

        For Each po As IList In prop_objs
            If po IsNot Nothing AndAlso po.Count > 0 Then
                Dim l As ILoadableList = TryCast(po, ILoadableList)
                If l IsNot Nothing Then
                    l.LoadObjects()
                End If
            End If
        Next
    End Sub

    Public Overloads Function GetRange(ByVal index As Integer, ByVal count As Integer) As ReadOnlyEntityList(Of T)
        If _l.Count > 0 Then
            Dim lst As List(Of T) = _l.GetRange(index, count)
            Return New ReadOnlyEntityList(Of T)(lst)
        Else
            Return Me
        End If
    End Function

    Friend Overrides Function CloneEmpty() As IReadOnlyList
        Return New ReadOnlyEntityList(Of T)
    End Function

    Public Overrides Function Clone() As Object
        Return New ReadOnlyEntityList(Of T)(_l)
    End Function

    Public Overrides ReadOnly Property RealType() As System.Type
        Get
            Return _rt
        End Get
    End Property

End Class

<Serializable()> _
Public Class ReadOnlyObjectList(Of T As {Entities._IEntity})
    Inherits ObjectModel.ReadOnlyCollection(Of T)
    Implements IListEdit, ComponentModel.ITypedList

    Protected _l As List(Of T)
    Friend _sl As New SpinLockRef

    Private ReadOnly Property _List() As IList Implements IListEdit.List
        Get
            Return _l
        End Get
    End Property

    Protected Friend ReadOnly Property List() As List(Of T)
        Get
            Return _l
        End Get
    End Property

    Public Sub New()
        MyClass.New(New List(Of T))
    End Sub

    Public Sub New(ByVal col As IEnumerable(Of T))
        MyClass.New(New List(Of T)(col))
    End Sub

    Public Sub New(ByVal list As List(Of T))
        MyBase.New(list)
        _l = list
    End Sub

    Public Sub New(ByVal list As ReadOnlyObjectList(Of T))
        MyClass.New(New List(Of T)(list))
    End Sub

    Public Sub New(ByVal t As Type)
        MyClass.new()
    End Sub

    Public Sub New(ByVal t As Type, ByVal col As IEnumerable(Of T))
        MyClass.New(col)
    End Sub

    Public Sub New(ByVal t As Type, ByVal list As List(Of T))
        MyClass.New(list)
    End Sub

    Public Sub New(ByVal t As Type, ByVal list As ReadOnlyObjectList(Of T))
        MyClass.New(list)
    End Sub

    Public Sub Sort(ByVal cs As IComparer(Of T))
        Using New CSScopeMgrLite(_sl)
            _l.Sort(cs)
        End Using
    End Sub

    Private Sub _Add(ByVal o As Entities.IEntity) Implements IListEdit.Add
        Using New CSScopeMgrLite(_sl)
            CType(_l, IList).Add(o)
        End Using
        RaiseEvent CollectionChanged(Me, New NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, o))
        RaiseEvent PropertyChanged(Me, New comp.PropertyChangedEventArgs("Count"))
        RaiseEvent PropertyChanged(Me, New comp.PropertyChangedEventArgs("Item[]"))
    End Sub

    Private Overloads Sub Insert(ByVal pos As Integer, ByVal o As Entities.IEntity) Implements IListEdit.Insert
        Using New CSScopeMgrLite(_sl)
            CType(_l, IList).Insert(pos, o)
        End Using
        RaiseEvent CollectionChanged(Me, New NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, o, pos))
        RaiseEvent PropertyChanged(Me, New comp.PropertyChangedEventArgs("Count"))
        RaiseEvent PropertyChanged(Me, New comp.PropertyChangedEventArgs("Item[]"))
    End Sub

    Private Overloads Sub Remove(ByVal o As Entities.IEntity) Implements IListEdit.Remove
        Dim pos As Integer = 0
        Using New CSScopeMgrLite(_sl)
            pos = CType(_l, IList).IndexOf(o)
            CType(_l, IList).RemoveAt(pos)
        End Using
        RaiseEvent CollectionChanged(Me, New NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, o, pos))
        RaiseEvent PropertyChanged(Me, New comp.PropertyChangedEventArgs("Count"))
        RaiseEvent PropertyChanged(Me, New comp.PropertyChangedEventArgs("Item[]"))
    End Sub

    Public Function ApplyFilter(ByVal filter As IGetFilter, joins As QueryJoin(), objEU As EntityUnion, ByRef evaluated As Boolean) As ReadOnlyObjectList(Of T)
        If _l.Count > 0 Then
            Dim o As T = _l(0)
            Using mc As IGetManager = o.GetMgr()
                Return mc.Manager.ApplyFilter(Of T)(Me, filter, joins, objEU, evaluated)
            End Using
        Else
            Return Me
        End If
    End Function

    Public Function ApplyFilter(ByVal filter As IGetFilter, ByRef evaluated As Boolean) As ReadOnlyObjectList(Of T)
        If _l.Count > 0 Then
            Dim o As T = _l(0)
            Using mc As IGetManager = o.GetMgr()
                Return mc.Manager.ApplyFilter(Of T)(Me, filter, Nothing, Nothing, evaluated)
            End Using
        Else
            Return Me
        End If
    End Function

    Public Function ApplyFilter(ByVal filter As IGetFilter, joins As QueryJoin(), objEU As EntityUnion) As ReadOnlyObjectList(Of T)
        If _l.Count > 0 Then
            Dim evaluated As Boolean
            Dim o As T = _l(0)
            Using mc As IGetManager = o.GetMgr()
                Dim r As ReadOnlyObjectList(Of T) = mc.Manager.ApplyFilter(Of T)(Me, filter, joins, objEU, evaluated)
                If Not evaluated Then
                    Throw New InvalidOperationException("Filter is not applyable")
                End If
                Return r
            End Using
        Else
            Return Me
        End If
    End Function

    Public Function ApplyFilter(ByVal filter As IGetFilter) As ReadOnlyObjectList(Of T)
        If _l.Count > 0 Then
            Dim evaluated As Boolean
            Dim o As T = _l(0)
            Using mc As IGetManager = o.GetMgr()
                Dim r As ReadOnlyObjectList(Of T) = mc.Manager.ApplyFilter(Of T)(Me, filter, Nothing, Nothing, evaluated)
                If Not evaluated Then
                    Throw New InvalidOperationException("Filter is not applyable")
                End If
                Return r
            End Using
        Else
            Return Me
        End If
    End Function

    Public Function ApplySort(ByVal s As OrderByClause) As ICollection(Of T)
        Return OrmManager.ApplySort(Of T)(Me, s)
    End Function

    Public Function GetRange(ByVal index As Integer, ByVal count As Integer) As ReadOnlyObjectList(Of T)
        If _l.Count > 0 Then
            Using New CSScopeMgrLite(_sl)
                Dim lst As List(Of T) = _l.GetRange(index, count)
                Return New ReadOnlyObjectList(Of T)(lst)
            End Using
        Else
            Return Me
        End If
    End Function

    Public Function GetItemProperties(ByVal listAccessors() As System.ComponentModel.PropertyDescriptor) As System.ComponentModel.PropertyDescriptorCollection Implements System.ComponentModel.ITypedList.GetItemProperties
        If GetType(Entities.AnonymousEntity).IsAssignableFrom(GetType(T)) AndAlso Count > 0 Then
            Return CType(Me(0), ComponentModel.ICustomTypeDescriptor).GetProperties
        Else
            Return Nothing
        End If
    End Function

    Public Function GetListName(ByVal listAccessors() As System.ComponentModel.PropertyDescriptor) As String Implements System.ComponentModel.ITypedList.GetListName
        Return Nothing
    End Function

    Friend Overridable Function CloneEmpty() As IReadOnlyList Implements IReadOnlyList.CloneEmpty
        Return New ReadOnlyObjectList(Of T)()
    End Function

    Public Overridable Function Clone() As Object Implements System.ICloneable.Clone
        Using New CSScopeMgrLite(_sl)
            Return New ReadOnlyObjectList(Of T)(_l)
        End Using
    End Function

    Public Overridable ReadOnly Property RealType() As System.Type Implements IReadOnlyList.RealType
        Get
            Return GetType(T)
        End Get
    End Property

    Public Function SelectEntity(Of EntityType As ISinglePKEntity)(ByVal propertyAlias As String) As ReadOnlyList(Of EntityType)
        Return SelectEntity(Of EntityType)(0, Count, propertyAlias)
    End Function

    Public Function SelectEntity(Of EntityType As ISinglePKEntity)(ByVal start As Integer, ByVal length As Integer, ByVal propertyAlias As String) As ReadOnlyList(Of EntityType)
        Dim r As IListEdit = Nothing
        Dim mpe As ObjectMappingEngine = Nothing
        Dim oschema As IEntitySchema = Nothing
        Using New CSScopeMgrLite(_sl)
            For i As Integer = 0 To Me.Count - 1
                If start <= i AndAlso start + length > i Then
                    Dim o As T = Me(i)
                    If mpe Is Nothing Then
                        mpe = o.GetMappingEngine
                        oschema = mpe.GetEntitySchema(o.GetType)
                    End If
                    Dim obj As IEntity = CType(ObjectMappingEngine.GetPropertyValue(o, propertyAlias, oschema), IEntity)
                    If obj IsNot Nothing Then
                        If r Is Nothing Then
                            r = OrmManager._CreateReadOnlyList(GetType(EntityType), obj.GetType)
                        End If
                        r.Add(obj)
                    End If
                End If
            Next
        End Using
        Return CType(r, ReadOnlyList(Of EntityType))
    End Function

    Public Function SelectProperties(ByVal start As Integer, ByVal length As Integer, ByVal ParamArray propertyAliases() As String) As ReadOnlyEntityList(Of Entities.AnonymousCachedEntity)
        Dim r As IListEdit = New ReadOnlyEntityList(Of AnonymousCachedEntity)
        Dim mpe As ObjectMappingEngine = Nothing
        Dim oschema As IEntitySchema = Nothing
        Using New CSScopeMgrLite(_sl)
            For i As Integer = 0 To Me.Count - 1
                If start <= i AndAlso start + length > i Then
                    Dim o As T = Me(i)
                    If mpe Is Nothing Then
                        mpe = o.GetMappingEngine
                        oschema = mpe.GetEntitySchema(o.GetType)
                    End If
                    Dim obj As New AnonymousCachedEntity
                    For Each propertyAlias As String In propertyAliases
                        Dim propValue As Object = ObjectMappingEngine.GetPropertyValue(o, propertyAlias, oschema)
                        obj(propertyAlias) = propValue
                    Next
                    r.Add(obj)
                End If
            Next
        End Using
        Return CType(r, ReadOnlyEntityList(Of AnonymousCachedEntity))
    End Function

    Public Function Cast(Of CastType)() As IList(Of CastType)
        If GetType(Entities._IEntity).IsAssignableFrom(GetType(CastType)) Then
            Dim l As IListEdit = CType(OrmManager.CreateReadOnlyList(GetType(CastType), RealType), IListEdit)
            Using New CSScopeMgrLite(_sl)
                For Each e As Object In Me
                    l.Add(CType(e, _IEntity))
                Next
            End Using
            Return CType(l, IList(Of CastType))
        Else
            Dim l As New List(Of CastType)
            Using New CSScopeMgrLite(_sl)
                For Each e As Object In Me
                    l.Add(CType(e, CastType))
                Next
            End Using
            Return l
        End If
    End Function

    Public Event CollectionChanged(sender As Object, e As System.Collections.Specialized.NotifyCollectionChangedEventArgs) Implements System.Collections.Specialized.INotifyCollectionChanged.CollectionChanged

    Public Event PropertyChanged(sender As Object, e As System.ComponentModel.PropertyChangedEventArgs) Implements System.ComponentModel.INotifyPropertyChanged.PropertyChanged
End Class