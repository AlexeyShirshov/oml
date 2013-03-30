Imports Worm.Criteria.Core
Imports Worm.Query.Sorting
Imports Worm.Entities
Imports System.Collections.Generic
Imports Worm.Entities.Meta
Imports Worm.Expressions2
Imports Worm.Criteria.Joins
Imports Worm.Query

Namespace Cache

    <Serializable()> _
    Public Class CachedItemBase
        Friend _expires As Date
        Protected _execTime As TimeSpan
        Protected _fetchTime As TimeSpan
        Protected _sort As OrderByClause
        Private _customInfo As Object

        Protected _col As ICollection

        Friend Sub New()
        End Sub

        Friend Sub New(ByVal col As ICollection, ByVal cache As CacheBase)
            _col = CType(cache.ListConverter.ToWeakList(col), System.Collections.ICollection)
            '_cache = cache
            If col IsNot Nothing Then cache.RegisterCreationCacheItem(Me.GetType)
        End Sub

        Friend Sub New(ByVal r As ReadonlyMatrix, ByVal cache As CacheBase)
            _col = CType(cache.ListConverter.ToWeakList(r), System.Collections.ICollection)
            If r IsNot Nothing Then cache.RegisterCreationCacheItem(Me.GetType)
        End Sub

        Public Overridable Function GetCount(ByVal cache As CacheBase) As Integer
            Return _col.Count
        End Function

        Public Overridable Function GetAliveCount(ByVal cache As CacheBase) As Integer
            Return cache.ListConverter.GetAliveCount(_col)
        End Function

        Public Overridable Function GetObjectList(Of T)(ByVal mgr As OrmManager, _
            ByVal start As Integer, ByVal length As Integer) As IList(Of T)
            If Not (start = 0 AndAlso (_col.Count = length OrElse length = Integer.MaxValue)) Then
                Dim list As IList = TryCast(_col, IList)
                Dim r As New List(Of T)
                If list IsNot Nothing Then
                    For i As Integer = start To Math.Min(start + length, _col.Count) - 1
                        r.Add(CType(list(i), T))
                    Next
                Else
                    Dim i As Integer = 0
                    For Each o As T In _col
                        If i >= start Then
                            r.Add(o)
                        End If
                        If i >= length + start Then
                            Exit For
                        End If
                        i += 1
                    Next
                End If
                Return New List(Of T)(r)
            Else
                Return CType(_col, IList(Of T))
            End If
        End Function

        Public Overridable Function GetObjectList(Of T As {_IEntity})(ByVal mgr As OrmManager, _
            ByVal withLoad As Boolean, ByVal created As Boolean, _
            ByVal start As Integer, ByVal length As Integer, _
            ByRef successed As IListObjectConverter.ExtractListResult) As ReadOnlyObjectList(Of T)
            If Not (start = 0 AndAlso (_col.Count = length OrElse length = Integer.MaxValue)) Then
                Dim list As IList = TryCast(_col, IList)
                Dim r As New List(Of T)
                If list IsNot Nothing Then
                    For i As Integer = start To Math.Min(start + length, _col.Count) - 1
                        r.Add(CType(list(i), T))
                    Next
                Else
                    Dim i As Integer = 0
                    For Each o As T In _col
                        If i >= start Then
                            r.Add(o)
                        End If
                        If i >= length + start Then
                            Exit For
                        End If
                        i += 1
                    Next
                End If
                If mgr.GetRev Then
                    r.Reverse()
                End If

                Return New ReadOnlyObjectList(Of T)(r)
            Else
                Return CType(_col, ReadOnlyObjectList(Of T))
            End If
        End Function

        Public Overridable Function GetMatrix(ByVal mgr As OrmManager, _
            ByVal withLoad() As Boolean, ByVal created As Boolean, _
            ByVal start As Integer, ByVal length As Integer, _
            ByRef successed As IListObjectConverter.ExtractListResult) As ReadonlyMatrix
            Dim lc As IListObjectConverter = mgr.ListConverter
            Return lc.FromWeakList(_col, mgr, start, length, withLoad, created, successed)
            'If Not (start = 0 AndAlso (_col.Count = length OrElse length = Integer.MaxValue)) Then
            '    Dim list As IList = TryCast(_col, IList)
            '    Dim r As New List(Of T)
            '    If list IsNot Nothing Then
            '        For i As Integer = start To Math.Min(start + length, _col.Count) - 1
            '            r.Add(CType(list(i), T))
            '        Next
            '    Else
            '        Dim i As Integer = 0
            '        For Each o As T In _col
            '            If i >= start Then
            '                r.Add(o)
            '            End If
            '            If i >= length + start Then
            '                Exit For
            '            End If
            '            i += 1
            '        Next
            '    End If
            '    Return New ReadOnlyObjectList(Of T)(r)
            'Else
            '    Return CType(_col, ReadOnlyObjectList(Of T))
            'End If
        End Function

        Public ReadOnly Property ExecutionTime() As TimeSpan
            Get
                Return _execTime
            End Get
        End Property

        Public ReadOnly Property FetchTime() As TimeSpan
            Get
                Return _fetchTime
            End Get
        End Property

        Public Property Sort() As OrderByClause
            Get
                Return _sort
            End Get
            Friend Set(ByVal value As OrderByClause)
                'If value IsNot Nothing Then
                '    _sort = New SortExpression(value.Length - 1) {}
                '    Array.Copy(value, _sort, value.Length)
                'Else
                '    _sort = Nothing
                'End If
                _sort = value
            End Set
        End Property

        Public Sub Expire()
            _expires = Nothing
            'SortExpire()
        End Sub

        'Public Sub SortExpire()
        '    _sortExpires = Nothing
        'End Sub

        Public Function SortEquals(ByVal sort As OrderByClause, ByVal schema As ObjectMappingEngine, ByVal contextFilter As Object) As Boolean
            'If _sort Is Nothing Then
            '    If sort Is Nothing Then
            '        Return True
            '    Else
            '        Return False
            '    End If
            'ElseIf _sort.Key <> 0 Then
            '    Return _sort.Key = sort.GetOnlyKey(schema, contextFilter).Key
            'Else
            '    Return _sort.Equals(sort)
            'End If
            If _sort Is Nothing Then
                If sort Is Nothing Then
                    Return True
                Else
                    Return False
                End If
            Else
                If sort Is Nothing OrElse _sort.Count <> sort.Count Then
                    Return False
                Else
                    For i As Integer = 0 To _sort.Count - 1
                        If Not _sort(i).Equals(sort(i)) Then
                            Return False
                        End If
                    Next
                    Return True
                End If
            End If
        End Function

        Public ReadOnly Property Expires() As Boolean
            Get
                If _expires <> Date.MinValue Then
                    Return _expires < Date.Now
                End If
                Return False
            End Get
        End Property

        Public Property CustomInfo() As Object
            Get
                Return _customInfo
            End Get
            Set(ByVal value As Object)
                _customInfo = value
            End Set
        End Property

        Public Overridable Sub Clear(ByVal mgr As OrmManager)
            mgr.ListConverter.Clear(_col, mgr)
        End Sub
    End Class

    <Serializable()> _
    Public Class UpdatableCachedItem
        Inherits CachedItemBase
        Implements IApplyFilter

        'Protected _st As SortType
        'Protected _obj As Object
        'Protected _mark As Object
        'Protected _cache As OrmBase
        Protected _f As IFilter
        Protected _joins As QueryJoin()
        Protected _eu As EntityUnion
        'Protected Friend _expires As Date
        'Protected _sortExpires As Date
        'Protected _execTime As TimeSpan
        'Protected _fetchTime As TimeSpan

        'Public Sub New(ByVal sort As String, ByVal obj As Object)
        '    If sort Is Nothing Then sort = String.Empty
        '    _sort = sort
        '    _obj = obj
        'End Sub

        'Public Sub New(ByVal sort As String, ByVal sortType As SortType, ByVal obj As IEnumerable, ByVal mark As Object, ByVal mc As OrmManager)
        '    _sort = sort
        '    _st = sortType
        '    _obj = mc.ListConverter.ToWeakList(obj)
        '    _mark = mark
        '    _cache = mc.Cache
        '    If obj IsNot Nothing Then _cache.RegisterCreationCacheItem()
        'End Sub

        Protected Sub New()
        End Sub

        'Public Sub New(ByVal sort As Sort, ByVal sortExpire As Date, ByVal filter As IFilter, ByVal obj As IEnumerable, ByVal mgr As OrmManager)
        'Public Sub New(ByVal sortExpire As Date, ByVal obj As IEnumerable, ByVal mgr As OrmManager)
        '    'If sort IsNot Nothing Then
        '    '    _sort = CType(sort.Clone, Sorting.Sort)
        '    'End If
        '    '_st = sortType
        '    'Using p As New CoreFramework.Debuging.OutputTimer("To week list")
        '    _obj = mgr.ListConverter.ToWeakList(obj)
        '    'End Using
        '    '_cache = mgr.Cache
        '    If obj IsNot Nothing Then mgr.Cache.RegisterCreationCacheItem(Me.GetType)
        '    ' _f = filter
        '    _expires = mgr._expiresPattern
        '    '_sortExpires = sortExpire
        '    _execTime = mgr.Exec
        '    _fetchTime = mgr.Fecth
        'End Sub

        Friend Sub New(ByVal obj As ICollection, ByVal cache As CacheBase)
            MyBase.New(obj, cache)
            '_obj = obj
            '_cache = cache
            'If obj IsNot Nothing Then cache.RegisterCreationCacheItem(Me.GetType)
        End Sub

        'Public Sub New(ByVal filter As IFilter, ByVal obj As IEnumerable, ByVal mgr As OrmManager)
        Public Sub New(ByVal obj As ICollection, ByVal mgr As OrmManager)
            '_sort = Nothing
            '_st = sortType
            'Using p As New CoreFramework.Debuging.OutputTimer("To week list")
            '_obj = mgr.ListConverter.ToWeakList(obj)
            'End Using
            '_cache = mgr.Cache
            'If obj IsNot Nothing Then mgr.Cache.RegisterCreationCacheItem(Me.GetType)
            '_f = Filter
            MyBase.New(obj, mgr.Cache)
            _expires = mgr._expiresPattern
            _execTime = mgr.Exec
            _fetchTime = mgr.Fecth
        End Sub

        Public Overridable ReadOnly Property CanRenewAfterSort() As Boolean
            Get
                Return True
            End Get
        End Property

        'Public Shared Function CreateEmpty(ByVal sort As String) As CachedItem
        '    Return New CachedItem(sort, Nothing, Nothing, OrmManager.CurrentManager)
        'End Function

        'Public Overrides Function Equals(ByVal obj As Object) As Boolean
        '    Return Equals(TryCast(obj, CachedItem))
        'End Function

        'Public Overrides Function GetHashCode() As Integer
        '    Return MyBase.GetHashCode()
        'End Function

        'Protected Function GetSortExp() As String
        '    If _sort IsNot Nothing Then
        '        Return _sort.ToString
        '    End If
        '    Return Nothing
        'End Function

        'Public Overloads Function Equals(ByVal obj As CachedItem) As Boolean
        '    If obj Is Nothing Then Return False

        '    Dim sortExpression As String = GetSortExp()
        '    If Not String.IsNullOrEmpty(sortExpression) Then
        '        'If _mark Is Nothing Then
        '        Return sortExpression.Equals(obj.GetSortExp)
        '        'ElseIf obj._mark IsNot Nothing Then
        '        '    Return sortExpression.Equals(obj.GetSortExp) AndAlso _mark.Equals(obj._mark)
        '        'End If
        '    ElseIf String.IsNullOrEmpty(obj.GetSortExp) Then
        '        Return True
        '    End If
        '    Return False
        'End Function

        Public Overrides Function GetCount(ByVal cache As CacheBase) As Integer
            Return cache.ListConverter.GetCount(_col)
        End Function

        'Public Overrides Function GetAliveCount(ByVal cache As CacheBase) As Integer
        '    Return cache.ListConverter.GetAliveCount(_col)
        'End Function

        'Public ReadOnly Property SortExpires() As Boolean
        '    Get
        '        If _sortExpires <> Date.MinValue Then
        '            Return _sortExpires < Date.Now
        '        End If
        '        Return False
        '    End Get
        'End Property

        'Public Overrides Function GetObjectList(Of T As {_IEntity})(ByVal mgr As OrmManager, _
        '    ByVal withLoad As Boolean, ByVal created As Boolean, _
        '    ByVal start As Integer, ByVal length As Integer, _
        '    ByRef successed As IListObjectConverter.ExtractListResult) As ReadOnlyObjectList(Of T)

        'End Function

        Public Overridable Overloads Function GetObjectList(Of T As ICachedEntity)(ByVal mgr As OrmManager, _
            ByVal withLoad As Boolean, ByVal created As Boolean, _
            ByVal start As Integer, ByVal length As Integer, _
            ByRef successed As IListObjectConverter.ExtractListResult) As ReadOnlyEntityList(Of T)
            Dim lc As IListObjectConverter = mgr.ListConverter
            Return lc.FromWeakList(Of T)(_col, mgr, start, length, withLoad, created, successed)
        End Function

        Public Overridable Overloads Function GetObjectList(Of T As ICachedEntity)( _
            ByVal mgr As OrmManager) As ReadOnlyEntityList(Of T)
            Dim lc As IListObjectConverter = mgr.ListConverter
            Return lc.FromWeakList(Of T)(_col, mgr)
        End Function

        'Public Sub SetObjectList(ByVal mc As OrmManager, ByVal value As OrmBase())
        '    _obj = mc.ListConverter.ToWeakList(value)
        'End Sub

        'Public ReadOnly Property Mark() As Object
        '    Get
        '        Return _mark
        '    End Get
        'End Property

        Shared Operator =(ByVal o1 As UpdatableCachedItem, ByVal o2 As UpdatableCachedItem) As Boolean
            If o1 Is Nothing Then
                If o2 Is Nothing Then
                    Return True
                Else
                    Return False
                End If
            Else
                Return o1.Equals(o2)
            End If
        End Operator

        Shared Operator <>(ByVal o1 As UpdatableCachedItem, ByVal o2 As UpdatableCachedItem) As Boolean
            Return Not (o1 = o2)
        End Operator

        'Protected Overrides Sub Finalize()
        '    If _obj IsNot Nothing Then _cache.RegisterRemovalCacheItem(Me)
        'End Sub

        Public Overridable Function Add(ByVal mgr As OrmManager, ByVal obj As ICachedEntity) As Boolean
            Return mgr.ListConverter.Add(_col, mgr, obj, _sort)
        End Function

        Public Overridable Sub Delete(ByVal mgr As OrmManager, ByVal obj As ICachedEntity)
            mgr.ListConverter.Delete(_col, obj, mgr)
        End Sub

        Public Property Filter() As IFilter
            Get
                Return _f
            End Get
            Friend Set(ByVal value As IFilter)
                _f = value
            End Set
        End Property

        Public Property Joins() As QueryJoin()
            Get
                Return _joins
            End Get
            Friend Set(ByVal value As QueryJoin())
                _joins = value
            End Set
        End Property

        Public Property QueryEU() As EntityUnion
            Get
                Return _eu
            End Get
            Friend Set(ByVal value As EntityUnion)
                _eu = value
            End Set
        End Property

        Private ReadOnly Property _Filter As Criteria.Core.IFilter Implements Criteria.Core.IApplyFilter.Filter
            Get
                Return Filter
            End Get
        End Property

        Private ReadOnly Property _JoinsImpl As Criteria.Joins.QueryJoin() Implements Criteria.Core.IApplyFilter.Joins
            Get
                Return Joins
            End Get
        End Property

        Private ReadOnly Property _RootObjectUnion As Query.EntityUnion Implements Criteria.Core.IApplyFilter.RootObjectUnion
            Get
                Return QueryEU
            End Get
        End Property
    End Class

#If OLDM2M Then

    <Serializable()> _
    Public Class M2MCache
        Inherits UpdatableCachedItem

        Public Sub New(ByVal mainId As Object, ByVal obj As IList(Of Object), ByVal mgr As OrmManager, _
            ByVal mainType As Type, ByVal subType As Type, ByVal direct As Boolean, ByVal sort As Sort)
            MyClass.new(mainId, obj, mgr, mainType, subType, Meta.M2MRelationDesc.GetKey(direct), sort)
        End Sub

        Public Sub New(ByVal mainId As Object, ByVal obj As IList(Of Object), ByVal mgr As OrmManager, _
            ByVal mainType As Type, ByVal subType As Type, ByVal key As String, ByVal sort As Sort)
            If sort IsNot Nothing Then
                _sort = CType(sort.Clone, Sort)
            End If

            '_st = sortType
            '_cache = mgr.Cache
            If obj IsNot Nothing Then
                mgr.Cache.RegisterCreationCacheItem(Me.GetType)
                _obj = New CachedM2MRelation(mainId, obj, mainType, subType, key, sort)
            End If
            '_f = filter
            _expires = mgr._expiresPattern
            _execTime = mgr.Exec
            _fetchTime = mgr.Fecth
        End Sub

        Public Sub New(ByVal sort As Sort, ByVal filter As IFilter, _
            ByVal el As CachedM2MRelation, ByVal mgr As OrmManager)
            If sort IsNot Nothing Then
                _sort = CType(sort.Clone, Sort)
            End If

            '_st = SortType
            '_cache = mgr.Cache
            If el IsNot Nothing Then
                mgr.Cache.RegisterCreationCacheItem(Me.GetType)
                _obj = el
            End If
            _f = filter
            _expires = mgr._expiresPattern
            _execTime = mgr.Exec
            _fetchTime = mgr.Fecth
        End Sub

        Public Overrides ReadOnly Property CanRenewAfterSort() As Boolean
            Get
                Return Not Entry.HasChanges
            End Get
        End Property

        'Public ReadOnly Property List() As EditableList
        '    Get
        '        Return CType(_obj, EditableList)
        '    End Get
        'End Property

        Public Function GetObjectListNonGeneric(ByVal mgr As OrmManager, _
            ByVal withLoad As Boolean, ByVal created As Boolean, ByRef successed As IListObjectConverter.ExtractListResult) As ICollection

            Dim flags As Reflection.BindingFlags = Reflection.BindingFlags.Instance Or Reflection.BindingFlags.Public

            Dim types As Type() = New Type() {GetType(OrmManager), GetType(Boolean), GetType(Boolean), GetType(IListObjectConverter.ExtractListResult)}

            Dim mi As Reflection.MethodInfo = GetType(OrmManager).GetMethod("GetObjectList", flags, Nothing, Reflection.CallingConventions.Any, types, Nothing)
            Dim mi_real As Reflection.MethodInfo = mi.MakeGenericMethod(New Type() {Entry.SubType})

            Return CType(mi_real.Invoke(Me, flags, Nothing, New Object() {mgr, withLoad, created, New IListObjectConverter.ExtractListResult}, Nothing), System.Collections.ICollection)
        End Function

        Public Overrides Function GetObjectList(Of T As {_ICachedEntity})(ByVal mgr As OrmManager, _
            ByVal withLoad As Boolean, ByVal created As Boolean, _
            ByVal start As Integer, ByVal length As Integer, _
            ByRef successed As IListObjectConverter.ExtractListResult) As ReadOnlyEntityList(Of T)
            successed = IListObjectConverter.ExtractListResult.Successed
            Dim tt As Type = GetType(T)
            Dim r As ReadOnlyEntityList(Of T) = CType(OrmManager._CreateReadOnlyList(tt), Global.Worm.ReadOnlyEntityList(Of T))
            If withLoad OrElse mgr._externalFilter IsNot Nothing Then
                Dim c As Integer = mgr.GetLoadedCount(tt, Entry.Current)
                Dim cnt As Integer = Entry.CurrentCount
#If TraceM2M Then
                Debug.WriteLine(Environment.StackTrace)
                Debug.WriteLine(cnt)
#End If
                If c < cnt Then
                    If mgr._externalFilter IsNot Nothing AndAlso Not OrmManager.IsGoodTime4Load(_fetchTime, _execTime, cnt, c) Then
                        successed = IListObjectConverter.ExtractListResult.NeedLoad
                        Return r
                    Else
                        r = mgr.LoadObjectsIds(Of T)(tt, Entry.Current, start, length)
                    End If
                Else
                    'r = mgr.LoadObjectsIds(Of T)(tt, Entry.Current, mgr.GetStart, mgr.GetLength)
                    r = CType(mgr.ConvertIds2Objects(tt, Entry.Current, start, length, False), Global.Worm.ReadOnlyEntityList(Of T))
                End If
                Dim s As Boolean = True
                r = CType(mgr.ApplyFilter(r, mgr._externalFilter, s), Global.Worm.ReadOnlyEntityList(Of T))
                If Not s Then
                    successed = IListObjectConverter.ExtractListResult.CantApplyFilter
                End If
            Else
                Try
#If TraceM2M Then
                    Debug.WriteLine(Entry.Current.Count)
#End If
                    r = CType(mgr.ConvertIds2Objects(tt, Entry.Current, start, length, False), Global.Worm.ReadOnlyEntityList(Of T))
                Catch ex As InvalidOperationException When ex.Message.StartsWith("Cannot prepare current data view")
                    r = CType(mgr.ConvertIds2Objects(tt, Entry.Original, start, length, False), Global.Worm.ReadOnlyEntityList(Of T))
                End Try
            End If
            Return r
        End Function

        Public Overrides Function GetObjectList(Of T As {_ICachedEntity})(ByVal mgr As OrmManager) As ReadOnlyEntityList(Of T)
            Try
#If TraceM2M Then
                Debug.WriteLine(Entry.Current.Count)
#End If
                Return CType(mgr.ConvertIds2Objects(GetType(T), Entry.Current, False), Global.Worm.ReadOnlyEntityList(Of T))
            Catch ex As InvalidOperationException When ex.Message.StartsWith("Cannot prepare current data view")
                Return CType(mgr.ConvertIds2Objects(GetType(T), Entry.Original, False), Global.Worm.ReadOnlyEntityList(Of T))
            End Try
        End Function

        Public Overrides Function Add(ByVal mgr As OrmManager, ByVal obj As ICachedEntity) As Boolean
            'If obj Is Nothing Then
            '    Throw New ArgumentNullException("obj")
            'End If
            'If Entry IsNot Nothing Then
            '    Entry.Add(obj.Identifier)
            '    Entry.Accept()
            'End If
            Throw New NotSupportedException
        End Function

        Public Overrides Sub Delete(ByVal mgr As OrmManager, ByVal obj As ICachedEntity)
            'If obj Is Nothing Then
            '    Throw New ArgumentNullException("obj")
            'End If
            'If Entry IsNot Nothing Then
            '    Entry.Delete(obj.Identifier)
            '    Entry.Accept()
            'End If
            Throw New NotSupportedException
        End Sub

        Public ReadOnly Property Entry() As CachedM2MRelation
            Get
                Return CType(_obj, CachedM2MRelation)
            End Get
        End Property

        Public Overrides Function GetCount(ByVal cache As CacheBase) As Integer
            Return Entry.CurrentCount
        End Function

        Public Overrides Function GetAliveCount(ByVal cache As CacheBase) As Integer
            Return 0
        End Function
    End Class

#End If

End Namespace