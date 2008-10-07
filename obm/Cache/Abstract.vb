Imports Worm.Orm

Namespace Cache
    Public Interface IExploreCache
        Function GetAllKeys() As ArrayList
        Function GetDictionary(ByVal key As Object) As IDictionary
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
End Namespace