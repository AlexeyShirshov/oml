﻿Imports Worm.Entities
Imports System.Collections.Generic

Namespace Cache
    Public Interface IExploreEntityCache
        Function GetAllKeys() As ArrayList
        Function GetDictionary(ByVal key As Object) As IDictionary
    End Interface

    Public Interface IExploreQueryCache
        Function GetAllKeys() As List(Of String)
        Function GetDictionary(ByVal key As String) As IDictionary
    End Interface

    Public Interface IUpdateCacheCallbacks
        Sub BeginUpdate(ByVal count As Integer)
        Sub EndUpdate()
        Sub BeginUpdateProcs()
        Sub EndUpdateProcs()
        Sub BeginUpdateList(ByVal key As String, ByVal id As String)
        Sub EndUpdateList(ByVal key As String, ByVal id As String)
        Sub ObjectDependsUpdated(ByVal o As ICachedEntity)
    End Interface

    Public Interface IUpdateCacheCallbacks2
        Sub BeginUpdate()
        Sub EndUpdate()
        Sub BeginUpdateProcs()
        Sub EndUpdateProcs()
        Sub BeginUpdateList(ByVal key As String, ByVal id As String)
        Sub EndUpdateList(ByVal key As String, ByVal id As String)
        Sub ObjectDependsUpdated(ByVal o As ICachedEntity)
    End Interface

    Public Interface IQueryDependentTypes
        Function [Get](ByVal mpe As ObjectMappingEngine) As IDependentTypes
    End Interface

    Public Interface IDependentTypes
        Function GetAddDelete() As IEnumerable(Of Type)
        Function GetUpdate() As IEnumerable(Of Type)
    End Interface

    Public Interface ICacheValidator
        Function ValidateItemFromCache(ByVal ce As UpdatableCachedItem) As Boolean
        Function ValidateBeforCacheProbe() As Boolean
    End Interface

End Namespace