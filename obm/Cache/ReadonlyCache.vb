Imports System.Collections.Generic
Imports Worm.Orm
Imports Worm.Orm.Meta

Namespace Cache
    Public Enum CacheListBehavior
        CacheAll
        CacheOrThrowException
        CacheWhatCan
    End Enum

    Public Enum ValidateBehavior
        Immediate
        Deferred
    End Enum

    Module qd
        Public Function QueryDependentTypes(ByVal o As Object) As IDependentTypes
            Dim qd As IQueryDependentTypes = TryCast(o, IQueryDependentTypes)
            If qd IsNot Nothing Then
                Return TryCast(qd, IDependentTypes)
            End If

            Return New EmptyDependentTypes
        End Function

        Public Function IsCalculated(ByVal dp As IDependentTypes) As Boolean
            If dp Is Nothing Then
                Return True
            Else
                Return dp.GetType IsNot GetType(EmptyDependentTypes)
            End If
        End Function

        Public Sub Add2Cache(ByVal cache As OrmCacheBase, ByVal dp As IDependentTypes, ByVal key As String, ByVal id As String)
            If dp IsNot Nothing Then
                For Each t As Type In dp.GetAddDelete
                    cache.validate_AddDeleteType(t, key, id)
                Next
                For Each t As Type In dp.GetUpdate
                    cache.validate_UpdateType(t, key, id)
                Next
            End If
        End Sub

        Public Function IsEmpty(ByVal dp As IDependentTypes) As Boolean
            Return Not (dp.GetAddDelete.GetEnumerator.MoveNext OrElse dp.GetUpdate.GetEnumerator.MoveNext)
        End Function
    End Module

    NotInheritable Class EmptyDependentTypes
        Implements IDependentTypes

        Public Function GetAddDelete() As System.Collections.Generic.IEnumerable(Of System.Type) Implements IDependentTypes.GetAddDelete
            Return Nothing
        End Function

        Public Function GetUpdate() As System.Collections.Generic.IEnumerable(Of System.Type) Implements IDependentTypes.GetUpdate
            Return Nothing
        End Function
    End Class

    Class DependentTypes
        Implements IDependentTypes

        Private _d As New List(Of Type)
        Private _u As New List(Of Type)

        Public Sub AddBoth(ByVal t As Type)
            AddDeleted(t)
            AddUpdated(t)
        End Sub

        Public Sub AddDeleted(ByVal t As Type)
            If Not _d.Contains(t) Then
                _d.Add(t)
            End If
        End Sub

        Public Sub AddUpdated(ByVal t As Type)
            If Not _u.Contains(t) Then
                _u.Add(t)
            End If
        End Sub

        Public Sub AddDeleted(ByVal col As IEnumerable(Of Type))
            For Each t As Type In col
                AddDeleted(t)
            Next
        End Sub

        Public Sub AddUpdated(ByVal col As IEnumerable(Of Type))
            For Each t As Type In col
                AddUpdated(t)
            Next
        End Sub

        Public Sub Merge(ByVal dp As IDependentTypes)
            If dp IsNot Nothing Then
                AddDeleted(dp.GetAddDelete)
                AddUpdated(dp.GetUpdate)
            End If
        End Sub

        Public ReadOnly Property IsEmpty() As Boolean
            Get
                Return _d.Count = 0 AndAlso _u.Count = 0
            End Get
        End Property

        Public Function [Get]() As IDependentTypes
            If IsEmpty Then
                Return Nothing
            End If
            Return Me
        End Function

        Public Function GetAddDelete() As System.Collections.Generic.IEnumerable(Of System.Type) Implements IDependentTypes.GetAddDelete
            Return _d
        End Function

        Public Function GetUpdate() As System.Collections.Generic.IEnumerable(Of System.Type) Implements IDependentTypes.GetUpdate
            Return _u
        End Function
    End Class

    Public Interface IQueryDependentTypes
        Function [Get](ByVal mpe As ObjectMappingEngine) As IDependentTypes
    End Interface

    Public Interface IDependentTypes
        Function GetAddDelete() As IEnumerable(Of Type)
        Function GetUpdate() As IEnumerable(Of Type)
    End Interface

    Public MustInherit Class ReadonlyCache

        Public ReadOnly DateTimeCreated As Date

        Private _filters As IDictionary
        Private _loadTimes As New Dictionary(Of Type, Pair(Of Integer, TimeSpan))
        Private _lock As New Object
        Private _list_converter As IListObjectConverter

        Public Event RegisterEntityCreation(ByVal e As IEntity)
        Public Event RegisterObjectCreation(ByVal t As Type, ByVal id As Integer)
        Public Event RegisterCollectionCreation(ByVal t As Type)
        Public Event RegisterCollectionRemoval(ByVal ce As OrmManager.CachedItem)

        Sub New()
            _filters = Hashtable.Synchronized(New Hashtable)
            DateTimeCreated = Now
            _list_converter = CreateListConverter()
        End Sub

        Public MustOverride Function CreateResultsetsDictionary() As IDictionary

        Public MustOverride Function GetOrmDictionary(ByVal filterInfo As Object, ByVal t As Type, ByVal schema As ObjectMappingEngine) As System.Collections.IDictionary

        Public MustOverride Function GetOrmDictionary(Of T)(ByVal filterInfo As Object, ByVal schema As ObjectMappingEngine) As System.Collections.Generic.IDictionary(Of Object, T)

        Public MustOverride Function GetOrmDictionary(ByVal filterInfo As Object, ByVal t As Type, ByVal schema As ObjectMappingEngine, ByVal oschema As IObjectSchemaBase) As System.Collections.IDictionary

        Public MustOverride Function GetOrmDictionary(Of T)(ByVal filterInfo As Object, ByVal schema As ObjectMappingEngine, ByVal oschema As IObjectSchemaBase) As System.Collections.Generic.IDictionary(Of Object, T)

        Public Overridable Sub Reset()
            _filters = Hashtable.Synchronized(New Hashtable)
        End Sub

        Public Overridable Function CreateResultsetsDictionary(ByVal mark As String) As IDictionary
            If String.IsNullOrEmpty(mark) Then
                Return CreateResultsetsDictionary()
            End If
            Throw New NotImplementedException(String.Format("Mark {0} is not supported", mark))
        End Function


        Public Overridable Sub RegisterCreation(ByVal obj As IEntity)
            RaiseEvent RegisterEntityCreation(obj)
        End Sub

        Public Overridable Sub RegisterCreation(ByVal t As Type, ByVal id As Integer)
            RaiseEvent RegisterObjectCreation(t, id)
#If TraceCreation Then
            _added.add(new Pair(Of date,Pair(Of type,Integer))(Now,New Pair(Of type,Integer)(t,id)))
#End If
        End Sub

        Public Overridable Property CacheListBehavior() As CacheListBehavior
            Get
                Return Cache.CacheListBehavior.CacheAll
            End Get
            Set(ByVal value As CacheListBehavior)
                'do nothing
            End Set
        End Property

        Public Overridable ReadOnly Property IsReadonly() As Boolean
            Get
                Return True
            End Get
        End Property

        Protected Overridable Function CreateListConverter() As IListObjectConverter
            Return New FakeListConverter
        End Function

        Public ReadOnly Property ListConverter() As IListObjectConverter
            Get
                Return _list_converter
            End Get
        End Property

        Public Overridable Sub RegisterCreationCacheItem(ByVal t As Type)
            RaiseEvent RegisterCollectionCreation(t)
        End Sub

        Public Overridable Sub RegisterRemovalCacheItem(ByVal ce As OrmManager.CachedItem)
            RaiseEvent RegisterCollectionRemoval(ce)
        End Sub

        Public Overridable Sub RemoveEntry(ByVal key As String, ByVal id As String)
            Dim dic As IDictionary = CType(_filters(key), System.Collections.IDictionary)
            If dic IsNot Nothing Then
                dic.Remove(id)
                If dic.Count = 0 Then
                    Using SyncHelper.AcquireDynamicLock(key)
                        If dic.Count = 0 Then
                            _filters.Remove(key)
                        End If
                    End Using
                End If
            End If
        End Sub

        Public Sub RemoveEntry(ByVal p As Pair(Of String))
            RemoveEntry(p.First, p.Second)
        End Sub

        Protected Function _GetDictionary(ByVal key As String) As IDictionary
            Return CType(_filters(key), IDictionary)
        End Function

        Public Function GetDictionary(ByVal key As String) As IDictionary
            Dim dic As IDictionary = CType(_filters(key), IDictionary)
            If dic Is Nothing Then
                Using SyncHelper.AcquireDynamicLock(key)
                    dic = CType(_filters(key), IDictionary)
                    If dic Is Nothing Then
                        dic = CreateResultsetsDictionary()
                        _filters.Add(key, dic)
                    End If
                End Using
            End If
            Return dic
        End Function

        Public Function GetDictionary(ByVal key As String, ByVal mark As String, ByRef created As Boolean) As IDictionary
            Dim dic As IDictionary = CType(_filters(key), IDictionary)
            created = False
            If dic Is Nothing Then
                Using SyncHelper.AcquireDynamicLock(key)
                    dic = CType(_filters(key), IDictionary)
                    If dic Is Nothing Then
                        dic = CreateResultsetsDictionary(mark)
                        _filters.Add(key, dic)
                        created = True
                    End If
                End Using
            End If
            Return dic
        End Function
    End Class
End Namespace