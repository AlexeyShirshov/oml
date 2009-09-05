Imports System
Imports System.Configuration.Install
Imports System.Diagnostics
Imports System.ComponentModel
Imports Worm.Entities
Imports Worm.Query.Sorting
Imports Worm.Entities.Meta
Imports System.Collections.Generic
Imports System.Collections.ObjectModel
Imports Worm.Expressions2

Namespace Cache

    Public Interface IListObjectConverter
        Enum ExtractListResult
            Successed
            NeedLoad
            CantApplyFilter
        End Enum

        Function FromWeakList(ByVal weak_list As Object, ByVal mgr As OrmManager, _
            ByVal start As Integer, ByVal length As Integer, _
            ByVal withLoad() As Boolean, _
            ByVal created As Boolean, ByRef successed As ExtractListResult) As ReadonlyMatrix

        Function ToWeakList(ByVal objects As IEnumerable) As Object
        Function FromWeakList(Of T As {_ICachedEntity})(ByVal weak_list As Object, ByVal mgr As OrmManager) As ReadOnlyEntityList(Of T)
        Function FromWeakList(Of T As {_ICachedEntity})(ByVal weak_list As Object, ByVal mgr As OrmManager, _
            ByVal start As Integer, ByVal length As Integer, ByVal withLoad As Boolean, ByVal created As Boolean, ByRef successed As ExtractListResult) As ReadOnlyEntityList(Of T)
        Function Add(ByVal weak_list As Object, ByVal mc As OrmManager, ByVal obj As ICachedEntity, ByVal sort As OrderByClause) As Boolean
        Function GetCount(ByVal weak_list As Object) As Integer
        Sub Delete(ByVal weak_list As Object, ByVal obj As ICachedEntity, ByVal mc As OrmManager)
        Sub Clear(ByVal weak_list As Object, ByVal mgr As OrmManager)
        ReadOnly Property IsWeak() As Boolean
        Function GetAliveCount(ByVal weakList As Object) As Integer

    End Interface

    Public Class FakeListConverter
        Implements IListObjectConverter

        Public Function FromWeakList(Of T As {_ICachedEntity})(ByVal weak_list As Object, ByVal mc As OrmManager, _
            ByVal start As Integer, ByVal length As Integer, ByVal withLoad As Boolean, ByVal created As Boolean, _
            ByRef successed As IListObjectConverter.ExtractListResult) As ReadOnlyEntityList(Of T) Implements IListObjectConverter.FromWeakList
            Dim c As ReadOnlyEntityList(Of T) = Nothing
            Try
                c = CType(weak_list, ReadOnlyEntityList(Of T))
            Catch ex As InvalidCastException
                Dim l As New Generic.List(Of T)
                Dim rt As Type = Nothing
                For Each o As T In CType(weak_list, IList)
                    l.Add(o)
                    rt = o.GetType
                Next
                c = CType(OrmManager._CreateReadOnlyList(GetType(T), rt, l), ReadOnlyEntityList(Of T))
            End Try
            successed = IListObjectConverter.ExtractListResult.Successed
            If withLoad AndAlso Not created Then
                c.LoadObjects(start, length)
                Dim s As Boolean = True
                c = CType(mc.ApplyFilter(Of T)(c, mc._externalFilter, s), Global.Worm.ReadOnlyEntityList(Of T))
                If Not s Then
                    successed = IListObjectConverter.ExtractListResult.CantApplyFilter
                End If
            ElseIf mc._externalFilter IsNot Nothing Then
                Dim er As OrmManager.ExecutionResult = mc.LastExecutionResult
                Dim l As Integer = 0
                If er.LoadedInResultset.HasValue Then
                    l = er.LoadedInResultset.Value
                Else
                    l = mc.GetLoadedCount(Of T)(c)
                End If
                If l < er.RowCount Then
                    'Dim tt As TimeSpan = er.FetchTime + er.ExecutionTime
                    ''Dim p As Pair(Of Integer, TimeSpan) = mc.Cache.GetLoadTime(GetType(T))
                    'Dim slt As Double = (er.FetchTime.TotalMilliseconds / er.Count)
                    'Dim ttl As TimeSpan = TimeSpan.FromMilliseconds(slt * (er.Count - l) * 1.1)
                    If OrmManager.IsGoodTime4Load(er.FetchTime, er.ExecutionTime, er.RowCount, l) Then
                        c.LoadObjects()
                    Else
                        successed = IListObjectConverter.ExtractListResult.NeedLoad
                        Return c
                    End If
                End If
                Dim s As Boolean = True
                c = CType(mc.ApplyFilter(Of T)(c, mc._externalFilter, s), Global.Worm.ReadOnlyEntityList(Of T))
                If Not s Then
                    successed = IListObjectConverter.ExtractListResult.CantApplyFilter
                End If
            End If
            If start < c.Count Then
                If Not (start = 0 AndAlso (c.Count = length OrElse length = Integer.MaxValue)) Then
                    If mc._externalFilter IsNot Nothing Then
                        Throw New InvalidOperationException("Paging is not supported with external filter")
                    End If
                    length = Math.Min(c.Count, length + start)
                    Dim ar As Generic.IList(Of T) = TryCast(c, Generic.IList(Of T))
                    Dim l As New Generic.List(Of T)
                    If ar IsNot Nothing Then
                        For i As Integer = start To length - 1
                            l.Add(ar(i))
                        Next
                    Else
                        Dim cnt As Integer = 0, s As Integer = 0
                        For Each o As T In c
                            If cnt >= start Then
                                l.Add(o)
                                s += 1
                            End If
                            If s >= length Then
                                Exit For
                            End If
                        Next
                    End If
                    If mc.GetRev Then
                        l.Reverse()
                    End If
                    c = CType(OrmManager._CreateReadOnlyList(GetType(T), l), Global.Worm.ReadOnlyEntityList(Of T))
                End If
            End If
            Return c
        End Function

        Public Function ToWeakList(ByVal objects As IEnumerable) As Object Implements IListObjectConverter.ToWeakList
            Return objects
        End Function

        Public Shared Function GetProp(ByVal rt As Type, ByVal obj As ICachedEntity, ByVal mc As OrmManager, ByVal oschema As IEntitySchema) As String
            Dim prop As String = mc.MappingEngine.GetJoinFieldNameByType(obj.GetType, rt, oschema)
            If String.IsNullOrEmpty(prop) Then
                For Each de As DictionaryEntry In mc.MappingEngine.GetRefProperties(obj.GetType, oschema)
                    Dim p As Reflection.PropertyInfo = CType(de.Value, Reflection.PropertyInfo)
                    If p.PropertyType.IsAssignableFrom(rt) Then
                        If Not String.IsNullOrEmpty(prop) Then
                            Throw New OrmManagerException(String.Format("Cannot get property of type {0} from {1}: Two properties match {2} and {3}", rt, obj.GetType, prop, CType(de.Key, EntityPropertyAttribute).PropertyAlias))
                        End If
                        prop = CType(de.Key, EntityPropertyAttribute).PropertyAlias
                    End If
                Next
            End If
            Return prop
        End Function

        Public Function Add(ByVal weak_list As Object, ByVal mc As OrmManager, ByVal o As ICachedEntity, _
            ByVal sort As OrderByClause) As Boolean Implements IListObjectConverter.Add
            Dim l As IListEdit = CType(weak_list, IListEdit)
            Dim obj As ICachedEntity = o
            If l.RealType IsNot obj.GetType Then
                Dim oschema As IEntitySchema = mc.MappingEngine.GetEntitySchema(obj.GetType)
                'Dim props As ICollection(Of String) = mc.MappingEngine.GetPropertyAliasByType(obj.GetType, l.RealType, oschema)
                'If props.Count <> 1 Then
                '    Throw New OrmManagerException(String.Format("Cannot get property of type {0} from {1}", l.RealType, obj.GetType))
                'End If
                'Dim prop As String = Nothing
                'For Each p As String In props
                '    prop = p
                'Next
                Dim prop As String = GetProp(l.RealType, obj, mc, oschema)
                obj = CType(mc.MappingEngine.GetPropertyValue(obj, prop, oschema), ICachedEntity)
                If obj.GetType IsNot l.RealType Then
                    If Not String.IsNullOrEmpty(prop) Then
                        Throw New OrmManagerException(String.Format("Cannot get property of type {0} from {1}", l.RealType, obj.GetType))
                    End If
                End If
            End If

            If sort Is Nothing Then
                l.Add(obj)
                Return True
            ElseIf OrmManager.CanSortOnClient(obj.GetType, l, sort) Then
                Dim c As IComparer = New EntityComparer(sort)
                If c IsNot Nothing Then
                    Dim pos As Integer = ArrayList.Adapter(l).BinarySearch(obj, c)
                    If pos < 0 Then
                        l.Insert(Not pos, obj)
                    End If
                    Return True
                End If
            End If

            Return False
        End Function

        Public Sub Delete(ByVal weak_list As Object, ByVal o As ICachedEntity, ByVal mc As OrmManager) Implements IListObjectConverter.Delete
            Dim l As IListEdit = CType(weak_list, IListEdit)
            Dim obj As ICachedEntity = o
            If l.RealType IsNot obj.GetType Then
                Dim oschema As IEntitySchema = mc.MappingEngine.GetEntitySchema(obj.GetType)
                'Dim props As ICollection(Of String) = mc.MappingEngine.GetPropertyAliasByType(obj.GetType, l.RealType, oschema)
                'If props.Count <> 1 Then
                '    Throw New OrmManagerException(String.Format("Cannot get property of type {0} from {1}", l.RealType, obj.GetType))
                'End If
                'Dim prop As String = Nothing
                'For Each p As String In props
                '    prop = p
                'Next
                Dim prop As String = GetProp(l.RealType, obj, mc, oschema)
                obj = CType(mc.MappingEngine.GetPropertyValue(obj, prop, oschema), ICachedEntity)
                If obj.GetType IsNot l.RealType Then
                    If Not String.IsNullOrEmpty(prop) Then
                        Throw New OrmManagerException(String.Format("Cannot get property of type {0} from {1}", l.RealType, obj.GetType))
                    End If
                End If
            End If
            l.Remove(obj)
        End Sub

        Public Sub Delete(ByVal weak_list As Object, ByVal mgr As OrmManager) Implements IListObjectConverter.Clear
            'do nothing
        End Sub

        Public Function GetCount(ByVal weak_list As Object) As Integer Implements IListObjectConverter.GetCount
            Dim l As IList = CType(weak_list, IList)
            Return l.Count
        End Function

        Public ReadOnly Property IsWeak() As Boolean Implements IListObjectConverter.IsWeak
            Get
                Return False
            End Get
        End Property

        Public Function FromWeakList(Of T As {_ICachedEntity})(ByVal weak_list As Object, ByVal mgr As OrmManager) As ReadOnlyEntityList(Of T) Implements IListObjectConverter.FromWeakList
            Dim c As ReadOnlyEntityList(Of T) = CType(weak_list, ReadOnlyEntityList(Of T))
            Return c
        End Function

        Public Function FromWeakList(ByVal weak_list As Object, ByVal mgr As OrmManager, ByVal start As Integer, ByVal length As Integer, ByVal withLoad() As Boolean, ByVal created As Boolean, ByRef successed As IListObjectConverter.ExtractListResult) As ReadonlyMatrix Implements IListObjectConverter.FromWeakList
            Dim m As ReadonlyMatrix = CType(weak_list, ReadonlyMatrix)
            If Not (start = 0 AndAlso (m.Count = length OrElse length = Integer.MaxValue)) Then
                Dim l As New List(Of ObjectModel.ReadOnlyCollection(Of _IEntity))
                For i As Integer = start To Math.Min(start + length, m.Count) - 1
                    Dim row As ReadOnlyCollection(Of _IEntity) = m(i)
                    l.Add(row)
                Next
                If mgr.GetRev Then
                    l.Reverse()
                End If
                m = New ReadonlyMatrix(l)
            End If

            Dim dic As New Dictionary(Of Type, IListEdit)
            For Each row As ReadOnlyCollection(Of _IEntity) In m
                For j As Integer = 0 To row.Count - 1
                    Dim e As _IEntity = row(j)
                    If e IsNot Nothing AndAlso withLoad(j) AndAlso Not e.IsLoaded Then
                        Dim t As Type = e.GetType
                        Dim o2l As IListEdit = Nothing
                        If Not dic.TryGetValue(t, o2l) Then
                            o2l = OrmManager._CreateReadOnlyList(t)
                            dic(t) = o2l
                        End If
                        o2l.Add(e)
                    End If
                Next
            Next

            For Each ll As ILoadableList In dic.Values
                ll.LoadObjects()
            Next

            Return m
        End Function

        Public Function GetAliveCount(ByVal weakList As Object) As Integer Implements IListObjectConverter.GetAliveCount
            Return CType(weakList, ICollection).Count
        End Function

    End Class

    Public Class ListConverter
        Implements IListObjectConverter

        Public Function FromWeakList(Of T As {_ICachedEntity})(ByVal weak_list As Object, ByVal mc As OrmManager, _
            ByVal start As Integer, ByVal length As Integer, ByVal withLoad As Boolean, ByVal created As Boolean, _
            ByRef successed As IListObjectConverter.ExtractListResult) As ReadOnlyEntityList(Of T) Implements IListObjectConverter.FromWeakList
            successed = IListObjectConverter.ExtractListResult.Successed
            If weak_list Is Nothing Then Return Nothing
            Dim lo As WeakEntityList = CType(weak_list, WeakEntityList)
            Dim l As Generic.List(Of WeakEntityReference) = lo.List
            Dim c As ReadOnlyEntityList(Of T) = CType(OrmManager._CreateReadOnlyList(GetType(T)), Global.Worm.ReadOnlyEntityList(Of T))
            Dim realT As Type = Nothing
            If l.Count > 0 Then
                realT = l(0).EntityType
            End If
            If realT IsNot Nothing Then
                Dim dic As IDictionary = mc.Cache.GetOrmDictionary(mc.GetContextInfo, realT, mc.MappingEngine)
                If mc._externalFilter Is Nothing Then
                    If start < l.Count Then
                        length = Math.Min(start + length, l.Count)
                        For i As Integer = start To length - 1
                            Dim loe As WeakEntityReference = l(i)
                            Dim o As T = loe.GetObject(Of T)(mc, dic)
                            If o IsNot Nothing Then
                                If mc.GetRev Then
                                    c.List.Insert(0, o)
                                Else
                                    c.List.Add(o)
                                End If
                            Else
                                OrmManager.WriteWarning("Unable to create " & loe.ObjName)
                            End If
                        Next
                        If withLoad AndAlso Not created Then
                            c.LoadObjects()
                        End If
                    End If
                Else
                    Dim loaded As Integer = 0
                    For Each loe As WeakEntityReference In l
                        If loe.IsLoaded Then
                            loaded += 1
                        End If
                        Dim o As T = loe.GetObject(Of T)(mc, dic)
                        If o IsNot Nothing Then
                            c.List.Add(o)
                        Else
                            OrmManager.WriteWarning("Unable to create " & loe.ObjName)
                        End If
                    Next
                    If loaded < l.Count Then
                        Dim er As OrmManager.ExecutionResult = mc.LastExecutionResult
                        If OrmManager.IsGoodTime4Load(er.FetchTime, er.ExecutionTime, er.RowCount, loaded) Then
                            'c = FromWeakList(Of T)(weak_list, mc)
                            c.LoadObjects()
                        Else
                            successed = IListObjectConverter.ExtractListResult.NeedLoad
                            Return CType(OrmManager._CreateReadOnlyList(GetType(T)), Global.Worm.ReadOnlyEntityList(Of T))
                        End If
                    End If
                    Dim s As Boolean = True
                    c = CType(mc.ApplyFilter(Of T)(c, mc._externalFilter, s), Global.Worm.ReadOnlyEntityList(Of T))
                    If Not s Then
                        successed = IListObjectConverter.ExtractListResult.CantApplyFilter
                    End If
                End If
            End If
            Return c
        End Function

        Public Function ToWeakList(ByVal objects As IEnumerable) As Object Implements IListObjectConverter.ToWeakList
            If objects Is Nothing Then Return Nothing
            If GetType(ReadonlyMatrix).IsAssignableFrom(objects.GetType) Then
                Dim r As ReadonlyMatrix = CType(objects, ReadonlyMatrix)
                Dim l As New List(Of WeakEntityList)
                For Each row As ReadOnlyCollection(Of _IEntity) In r
                    l.Add(CType(ToWeakList(row), WeakEntityList))
                Next
                Return New WeakEntityMatrix(l)
            Else
                Dim l As New Generic.List(Of WeakEntityReference)
                Dim t As Type = Nothing
                For Each o As ICachedEntity In objects
                    If t Is Nothing Then t = o.GetType
                    l.Add(New WeakEntityReference(o))
                Next
                Return New WeakEntityList(l, t)
            End If
        End Function

        Public Function Add(ByVal weak_list As Object, ByVal mc As OrmManager, ByVal o As ICachedEntity, _
            ByVal sort As OrderByClause) As Boolean Implements IListObjectConverter.Add
            Dim lo As WeakEntityList = CType(weak_list, WeakEntityList)
            Dim l As Generic.List(Of WeakEntityReference) = lo.List

            Dim obj As ICachedEntity = o
            If lo.RealType IsNot obj.GetType Then
                Dim oschema As IEntitySchema = mc.MappingEngine.GetEntitySchema(obj.GetType)
                'Dim props As ICollection(Of String) = mc.MappingEngine.GetPropertyAliasByType(obj.GetType, lo.RealType, oschema)
                'If props.Count <> 1 Then
                '    Throw New OrmManagerException(String.Format("Cannot get property of type {0} from {1}", lo.RealType, obj.GetType))
                'End If
                'Dim prop As String = Nothing
                'For Each p As String In props
                '    prop = p
                'Next
                Dim prop As String = FakeListConverter.GetProp(lo.RealType, obj, mc, oschema)
                obj = CType(mc.MappingEngine.GetPropertyValue(obj, prop, oschema), ICachedEntity)
                If obj.GetType IsNot lo.RealType Then
                    If Not String.IsNullOrEmpty(prop) Then
                        Throw New OrmManagerException(String.Format("Cannot get property of type {0} from {1}", lo.RealType, obj.GetType))
                    End If
                End If

            End If

            If sort Is Nothing Then
                l.Add(New WeakEntityReference(obj))
            Else
                Dim arr As ArrayList = Nothing
                If lo.CanSort(mc, arr, sort) Then
                    'Dim schema As IEntitySchema = mc.MappingEngine.GetEntitySchema(obj.GetType)
                    Dim c As IComparer = New EntityComparer(sort)
                    If c IsNot Nothing Then
                        Dim pos As Integer = ArrayList.Adapter(arr).BinarySearch(obj, c)
                        If pos < 0 Then
                            l.Insert(Not pos, New WeakEntityReference(obj))
                        End If
                        Return True
                    End If
                End If
            End If
            Return False
        End Function

        Public Sub Delete(ByVal weak_list As Object, ByVal o As ICachedEntity, ByVal mc As OrmManager) Implements IListObjectConverter.Delete
            Dim lo As WeakEntityList = CType(weak_list, WeakEntityList)

            Dim obj As ICachedEntity = o
            If lo.RealType IsNot obj.GetType Then
                Dim oschema As IEntitySchema = mc.MappingEngine.GetEntitySchema(obj.GetType)
                Dim prop As String = FakeListConverter.GetProp(lo.RealType, obj, mc, oschema)
                'Dim props As ICollection(Of String) = mc.MappingEngine.GetPropertyAliasByType(obj.GetType, lo.RealType, oschema)
                'If props.Count <> 1 Then
                '    Throw New OrmManagerException(String.Format("Cannot get property of type {0} from {1}", lo.RealType, obj.GetType))
                'End If
                'Dim prop As String = Nothing
                'For Each p As String In props
                '    prop = p
                'Next
                obj = CType(mc.MappingEngine.GetPropertyValue(obj, prop, oschema), ICachedEntity)
                If obj.GetType IsNot lo.RealType Then
                    If Not String.IsNullOrEmpty(prop) Then
                        Throw New OrmManagerException(String.Format("Cannot get property of type {0} from {1}", lo.RealType, obj.GetType))
                    End If
                End If
            End If

            lo.Remove(obj)
        End Sub

        Public Sub Delete(ByVal weak_list As Object, ByVal mgr As OrmManager) Implements IListObjectConverter.Clear
            Dim lo As WeakEntityList = CType(weak_list, WeakEntityList)

            lo.Clear(mgr)
        End Sub

        Public Function GetCount(ByVal weak_list As Object) As Integer Implements IListObjectConverter.GetCount
            Dim lo As WeakEntityList = TryCast(weak_list, WeakEntityList)
            If lo IsNot Nothing Then
                Return lo.Count
            Else
                Dim l As ICollection = CType(weak_list, ICollection)
                Return l.Count
            End If
        End Function

        Public ReadOnly Property IsWeak() As Boolean Implements IListObjectConverter.IsWeak
            Get
                Return True
            End Get
        End Property

        Public Function FromWeakList(Of T As {_ICachedEntity})(ByVal weak_list As Object, ByVal mgr As OrmManager) As ReadOnlyEntityList(Of T) Implements IListObjectConverter.FromWeakList
            If weak_list Is Nothing Then Return Nothing
            Dim lo As WeakEntityList = CType(weak_list, WeakEntityList)
            Dim l As Generic.List(Of WeakEntityReference) = lo.List
            Dim objects As New Generic.List(Of T)
            Dim dic As IDictionary = mgr.Cache.GetOrmDictionary(mgr.GetContextInfo, GetType(T), mgr.MappingEngine)
            For Each loe As WeakEntityReference In l
                Dim o As T = loe.GetObject(Of T)(mgr, dic)
                If o IsNot Nothing Then
                    objects.Add(o)
                Else
                    OrmManager.WriteWarning("Unable to create " & loe.ObjName)
                End If
            Next
            Return CType(OrmManager._CreateReadOnlyList(GetType(T), objects), Global.Worm.ReadOnlyEntityList(Of T))
        End Function

        Public Function FromWeakList(ByVal weak_list As Object, ByVal mgr As OrmManager, ByVal start As Integer, ByVal length As Integer, ByVal withLoad() As Boolean, ByVal created As Boolean, ByRef successed As IListObjectConverter.ExtractListResult) As ReadonlyMatrix Implements IListObjectConverter.FromWeakList
            If weak_list Is Nothing Then Return Nothing
            Dim wm As WeakEntityMatrix = CType(weak_list, WeakEntityMatrix)
            Dim r As New List(Of ReadOnlyCollection(Of _IEntity))
            Dim dic As New Dictionary(Of Type, IListEdit)
            For i As Integer = start To Math.Min(start + length, wm.Count) - 1
                Dim wl As WeakEntityList = wm(i)
                Dim row As New List(Of _IEntity)
                Dim odic As IDictionary = Nothing
                For j As Integer = 0 To wl.List.Count - 1
                    Dim wr As WeakEntityReference = wl.List(j)
                    If odic Is Nothing Then
                        odic = mgr.Cache.GetOrmDictionary(mgr.GetContextInfo, wr.EntityType, mgr.MappingEngine)
                    End If
                    Dim o As ICachedEntity = wr.GetObject(mgr, odic)
                    If o IsNot Nothing Then
                        row.Add(o)
                    Else
                        OrmManager.WriteWarning("Unable to create " & wr.ObjName)
                    End If

                    If withLoad(j) AndAlso Not o.IsLoaded Then
                        Dim t As Type = o.GetType
                        Dim o2l As IListEdit = Nothing
                        If Not dic.TryGetValue(t, o2l) Then
                            o2l = OrmManager._CreateReadOnlyList(t)
                            dic(t) = o2l
                        End If
                        o2l.Add(o)
                    End If
                Next

                r.Add(New ReadOnlyCollection(Of _IEntity)(row))
            Next

            For Each ll As ILoadableList In dic.Values
                ll.LoadObjects()
            Next

            If mgr.GetRev Then
                r.Reverse()
            End If

            Return New ReadonlyMatrix(r)
        End Function

        Public Function GetAliveCount(ByVal weakList As Object) As Integer Implements IListObjectConverter.GetAliveCount
            Dim sum As Integer
            If GetType(WeakEntityMatrix).IsAssignableFrom(weakList.GetType) Then
                For Each wl As WeakEntityList In CType(weakList, WeakEntityMatrix)
                    sum += GetAliveCount(wl)
                Next
            Else
                Dim lo As WeakEntityList = CType(weakList, WeakEntityList)
                For Each wr As WeakEntityReference In lo.List
                    If wr.IsAlive Then
                        sum += 1
                    End If
                Next
            End If
            Return sum
        End Function

    End Class

End Namespace
