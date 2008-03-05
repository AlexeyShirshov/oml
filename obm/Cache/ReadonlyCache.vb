Imports System.Collections.Generic
Imports Worm.Orm

Namespace Cache
    Public Class ReadonlyCache

        Public ReadOnly DateTimeCreated As Date

        Protected _filters As IDictionary
        Private _loadTimes As New Dictionary(Of Type, Pair(Of Integer, TimeSpan))

        Public Event RegisterObjectCreation(ByVal t As Type, ByVal id As Integer)
        Public Event RegisterObjectRemoval(ByVal obj As OrmBase)
        Public Event RegisterCollectionCreation(ByVal t As Type)
        Public Event RegisterCollectionRemoval(ByVal ce As OrmManagerBase.CachedItem)

        Sub New()
            _filters = Hashtable.Synchronized(New Hashtable)
            DateTimeCreated = Now
        End Sub
    End Class
End Namespace