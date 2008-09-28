Imports System
Imports System.Configuration.Install
Imports System.Diagnostics
Imports System.ComponentModel
Imports Worm.Orm
Imports Worm.Sorting
Imports Worm.Orm.Meta

Namespace Cache

    Public Interface IListObjectConverter
        Enum ExtractListResult
            Successed
            NeedLoad
            CantApplyFilter
        End Enum

        Function ToWeakList(ByVal objects As IEnumerable) As Object
        Function FromWeakList(Of T As {_ICachedEntity})(ByVal weak_list As Object, ByVal mgr As OrmManager) As ReadOnlyEntityList(Of T)
        Function FromWeakList(Of T As {_ICachedEntity})(ByVal weak_list As Object, ByVal mgr As OrmManager, _
            ByVal start As Integer, ByVal length As Integer, ByVal withLoad As Boolean, ByVal created As Boolean, ByRef successed As ExtractListResult) As ReadOnlyEntityList(Of T)
        Function Add(ByVal weak_list As Object, ByVal mc As OrmManager, ByVal obj As ICachedEntity, ByVal sort As Sort) As Boolean
        Function GetCount(ByVal weak_list As Object) As Integer
        Sub Delete(ByVal weak_list As Object, ByVal obj As ICachedEntity)
        ReadOnly Property IsWeak() As Boolean
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
                For Each o As T In CType(weak_list, IList)
                    l.Add(o)
                Next
                c = CType(OrmManager.CreateReadonlyList(GetType(T), l), ReadOnlyEntityList(Of T))
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
                Dim er As OrmManager.ExecutionResult = mc.GetLastExecitionResult
                Dim l As Integer = 0
                If er.LoadedInResultset.HasValue Then
                    l = er.LoadedInResultset.Value
                Else
                    l = mc.GetLoadedCount(Of T)(c)
                End If
                If l < er.Count Then
                    'Dim tt As TimeSpan = er.FetchTime + er.ExecutionTime
                    ''Dim p As Pair(Of Integer, TimeSpan) = mc.Cache.GetLoadTime(GetType(T))
                    'Dim slt As Double = (er.FetchTime.TotalMilliseconds / er.Count)
                    'Dim ttl As TimeSpan = TimeSpan.FromMilliseconds(slt * (er.Count - l) * 1.1)
                    If OrmManager.IsGoodTime4Load(er.FetchTime, er.ExecutionTime, er.Count, l) Then
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
                    c = CType(OrmManager.CreateReadonlyList(GetType(T), l), Global.Worm.ReadOnlyEntityList(Of T))
                End If
            End If
            Return c
        End Function

        Public Function ToWeakList(ByVal objects As IEnumerable) As Object Implements IListObjectConverter.ToWeakList
            Return objects
        End Function

        Public Function Add(ByVal weak_list As Object, ByVal mc As OrmManager, ByVal obj As ICachedEntity, _
            ByVal sort As Sort) As Boolean Implements IListObjectConverter.Add
            Dim l As IListEdit = CType(weak_list, IListEdit)
            Dim st As IOrmSorting = Nothing
            If sort Is Nothing Then
                l.Add(obj)
                Return True
            ElseIf mc.CanSortOnClient(obj.GetType, l, sort, st) Then
                Dim c As IComparer = Nothing
                If st IsNot Nothing Then
                    c = st.CreateSortComparer(sort)
                Else
                    c = New OrmComparer(Of _IEntity)(obj.GetType, sort)
                    'Dim ct As Type = GetType(OrmComparer(Of ))
                    'ct = ct.MakeGenericType(New Type() {})
                End If
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

        Public Sub Delete(ByVal weak_list As Object, ByVal obj As ICachedEntity) Implements IListObjectConverter.Delete
            Dim l As IListEdit = CType(weak_list, IListEdit)
            l.Remove(obj)
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
    End Class

    Public Class ListConverter
        Implements IListObjectConverter

        Class ListObjectEntry
            Private _e As EntityProxy
            Private ref As WeakReference

            Public Sub New(ByVal o As ICachedEntity)
                _e = New EntityProxy(o)
                ref = New WeakReference(o)
            End Sub

            Public Function GetObject(Of T As {_ICachedEntity})(ByVal mc As OrmManager) As T
                Dim o As T = CType(ref.Target, T)
                If o Is Nothing Then
                    o = CType(mc.GetEntityFromCacheOrCreate(_e.PK, GetType(T)), T) 'mc.FindObject(id, t)
                    If o Is Nothing AndAlso mc.NewObjectManager IsNot Nothing Then
                        o = CType(mc.NewObjectManager.GetNew(GetType(T), _e.PK), T)
                    End If
                End If
                Return o
            End Function

            Public Function GetObject(ByVal mc As OrmManager) As ICachedEntity
                Dim o As ICachedEntity = CType(ref.Target, ICachedEntity)
                If o Is Nothing Then
                    o = mc.GetEntityFromCacheOrCreate(_e.PK, _e.EntityType) 'mc.FindObject(id, t)
                    If o Is Nothing AndAlso mc.NewObjectManager IsNot Nothing Then
                        o = mc.NewObjectManager.GetNew(_e.EntityType, _e.PK)
                    End If
                End If
                Return o
            End Function

            Public ReadOnly Property ObjName() As String
                Get
                    Return _e.ToString
                End Get
            End Property

            Public ReadOnly Property IsLoaded() As Boolean
                Get
                    Return ref.IsAlive AndAlso CType(ref.Target, ICachedEntity).IsLoaded
                End Get
            End Property

            Public ReadOnly Property IsEqual(ByVal obj As ICachedEntity) As Boolean
                Get
                    Return New EntityProxy(obj).Equals(_e)
                End Get
            End Property
        End Class

        Public Class ListObject
            Public l As Generic.List(Of ListObjectEntry)
            Public t As Type

            Public Function CanSort(ByVal mc As OrmManager, ByRef arr As ArrayList, ByVal sort As Sort) As Boolean
                If sort.Previous IsNot Nothing Then
                    Return False
                End If

                arr = New ArrayList
                For Each le As ListObjectEntry In l
                    If Not le.IsLoaded Then
                        Return False
                    Else
                        arr.Add(le.GetObject(mc))
                    End If
                Next
                Return True
            End Function

            Sub Remove(ByVal obj As ICachedEntity)
                For i As Integer = 0 To l.Count - 1
                    Dim le As ListObjectEntry = l(i)
                    If le.IsEqual(obj) Then
                        l.RemoveAt(i)
                        Exit For
                    End If
                Next
            End Sub
        End Class

        Public Function FromWeakList(Of T As {_ICachedEntity})(ByVal weak_list As Object, ByVal mc As OrmManager, _
            ByVal start As Integer, ByVal length As Integer, ByVal withLoad As Boolean, ByVal created As Boolean, _
            ByRef successed As IListObjectConverter.ExtractListResult) As ReadOnlyEntityList(Of T) Implements IListObjectConverter.FromWeakList
            successed = IListObjectConverter.ExtractListResult.Successed
            If weak_list Is Nothing Then Return Nothing
            Dim lo As ListObject = CType(weak_list, ListObject)
            Dim l As Generic.List(Of ListObjectEntry) = lo.l
            Dim c As ReadOnlyEntityList(Of T) = CType(OrmManager.CreateReadonlyList(GetType(T)), Global.Worm.ReadOnlyEntityList(Of T))
            If mc._externalFilter Is Nothing Then
                If start < l.Count Then
                    length = Math.Min(start + length, l.Count)
                    For i As Integer = start To length - 1
                        Dim loe As ListObjectEntry = l(i)
                        Dim o As T = loe.GetObject(Of T)(mc)
                        If o IsNot Nothing Then
                            c.List.Add(o)
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
                For Each loe As ListObjectEntry In l
                    If loe.IsLoaded Then
                        loaded += 1
                    End If
                    Dim o As T = loe.GetObject(Of T)(mc)
                    If o IsNot Nothing Then
                        c.List.Add(o)
                    Else
                        OrmManager.WriteWarning("Unable to create " & loe.ObjName)
                    End If
                Next
                If loaded < l.Count Then
                    Dim er As OrmManager.ExecutionResult = mc.GetLastExecitionResult
                    If OrmManager.IsGoodTime4Load(er.FetchTime, er.ExecutionTime, er.Count, loaded) Then
                        'c = FromWeakList(Of T)(weak_list, mc)
                        c.LoadObjects()
                    Else
                        successed = IListObjectConverter.ExtractListResult.NeedLoad
                        Return CType(OrmManager.CreateReadonlyList(GetType(T)), Global.Worm.ReadOnlyEntityList(Of T))
                    End If
                End If
                Dim s As Boolean = True
                c = CType(mc.ApplyFilter(Of T)(c, mc._externalFilter, s), Global.Worm.ReadOnlyEntityList(Of T))
                If Not s Then
                    successed = IListObjectConverter.ExtractListResult.CantApplyFilter
                End If
            End If
            Return c
        End Function

        Public Function ToWeakList(ByVal objects As IEnumerable) As Object Implements IListObjectConverter.ToWeakList
            If objects Is Nothing Then Return Nothing
            Dim l As New Generic.List(Of ListObjectEntry)
            Dim t As Type = Nothing
            For Each o As ICachedEntity In objects
                If t Is Nothing Then t = o.GetType
                l.Add(New ListObjectEntry(o))
            Next
            Dim lo As New ListObject
            lo.l = l
            lo.t = t
            Return lo
        End Function

        Public Function Add(ByVal weak_list As Object, ByVal mc As OrmManager, ByVal obj As ICachedEntity, _
            ByVal sort As Sort) As Boolean Implements IListObjectConverter.Add
            Dim lo As ListObject = CType(weak_list, ListObject)
            Dim l As Generic.List(Of ListObjectEntry) = lo.l

            If sort Is Nothing Then
                l.Add(New ListObjectEntry(obj))
            Else
                Dim arr As ArrayList = Nothing
                If lo.CanSort(mc, arr, sort) Then
                    Dim schema As IContextObjectSchema = mc.MappingEngine.GetObjectSchema(obj.GetType)
                    Dim st As IOrmSorting = TryCast(schema, IOrmSorting)
                    Dim c As IComparer = Nothing
                    If st IsNot Nothing Then
                        c = st.CreateSortComparer(sort)
                    Else
                        c = New OrmComparer(Of _IEntity)(obj.GetType, sort)
                    End If
                    If c IsNot Nothing Then
                        Dim pos As Integer = ArrayList.Adapter(arr).BinarySearch(obj, c)
                        If pos < 0 Then
                            l.Insert(Not pos, New ListObjectEntry(obj))
                        End If
                        Return True
                    End If
                End If
            End If

            Return False
        End Function

        Public Sub Delete(ByVal weak_list As Object, ByVal obj As ICachedEntity) Implements IListObjectConverter.Delete
            Dim lo As ListObject = CType(weak_list, ListObject)

            lo.Remove(obj)
        End Sub

        Public Function GetCount(ByVal weak_list As Object) As Integer Implements IListObjectConverter.GetCount
            Dim lo As ListObject = CType(weak_list, ListObject)
            Return lo.l.Count
        End Function

        Public ReadOnly Property IsWeak() As Boolean Implements IListObjectConverter.IsWeak
            Get
                Return True
            End Get
        End Property

        Public Function FromWeakList(Of T As {_ICachedEntity})(ByVal weak_list As Object, ByVal mgr As OrmManager) As ReadOnlyEntityList(Of T) Implements IListObjectConverter.FromWeakList
            If weak_list Is Nothing Then Return Nothing
            Dim lo As ListObject = CType(weak_list, ListObject)
            Dim l As Generic.List(Of ListObjectEntry) = lo.l
            Dim objects As New Generic.List(Of T)
            For Each loe As ListObjectEntry In l
                Dim o As T = loe.GetObject(Of T)(mgr)
                If o IsNot Nothing Then
                    objects.Add(o)
                Else
                    OrmManager.WriteWarning("Unable to create " & loe.ObjName)
                End If
            Next
            Return CType(OrmManager.CreateReadonlyList(GetType(T), objects), Global.Worm.ReadOnlyEntityList(Of T))
        End Function
    End Class

End Namespace
