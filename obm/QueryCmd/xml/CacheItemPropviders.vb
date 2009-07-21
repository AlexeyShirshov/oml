Imports Worm.Entities
Imports Worm.Criteria.Core
Imports System.Collections.Generic
Imports Worm.Entities.Meta
Imports Worm.Xml
Imports System.Xml.XPath
Imports Worm.Criteria.Joins
Imports Worm.Cache

Namespace Query.Xml
    Partial Public Class XmlQueryExecutor
        Class ProviderT(Of CreateType As {ICachedEntity, New}, ReturnType As {ICachedEntity})
            Inherits CacheItemBaseProvider

            Public Sub New(ByVal mgr As QueryManager, ByVal q As QueryCmd)
                MyBase.New(mgr, q)
            End Sub

            Public Function GetEntities() As ReadOnlyObjectList(Of ReturnType)
                Dim r As ReadOnlyObjectList(Of ReturnType) = Nothing
                Dim dbm As QueryManager = CType(_mgr, QueryManager)

                Dim values As New List(Of ReturnType)
                Dim xpath As String = Nothing
                dbm.LoadMultipleObjects(Of CreateType)(xpath, values)

                r = New ReadOnlyObjectList(Of ReturnType)(values)

                Return r
            End Function

            Public Overrides Function GetCacheItem(ByVal ctx As TypeWrap(Of Object)) As CachedItemBase
                Return New UpdatableCachedItem(GetEntities(), _mgr.Cache)
            End Function

            Public Overrides Sub Reset(ByVal mgr As OrmManager, ByVal q As QueryCmd)
                Throw New NotImplementedException
            End Sub
        End Class
    End Class
End Namespace