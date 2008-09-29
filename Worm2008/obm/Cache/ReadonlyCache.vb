Imports System.Collections.Generic
Imports Worm.Orm
Imports Worm.Orm.Meta

Namespace Cache
    Public Enum CacheListBehavior
        CacheAll
        CacheOrThrowException
        CacheWhatCan
    End Enum

    Public MustInherit Class ReadonlyCache

        Public ReadOnly DateTimeCreated As Date

        Protected _filters As IDictionary
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

        Public MustOverride Sub Reset()

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
    End Class
End Namespace