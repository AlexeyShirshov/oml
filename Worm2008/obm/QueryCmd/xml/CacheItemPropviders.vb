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

            Public Sub New(ByVal mgr As QueryManager, ByVal j As List(Of List(Of Worm.Criteria.Joins.QueryJoin)), _
                ByVal f() As IFilter, ByVal q As QueryCmd, ByVal sl As List(Of List(Of SelectExpression)))
                MyBase.New(mgr, j, f, q, sl)
            End Sub

            Public Function GetEntities() As ReadOnlyObjectList(Of ReturnType)
                Dim r As ReadOnlyObjectList(Of ReturnType) = Nothing
                Dim dbm As QueryManager = CType(_mgr, QueryManager)

                Dim values As New List(Of ReturnType)
                Dim xpath As String = Nothing
                dbm.LoadMultipleObjects(Of CreateType)(xpath, values)

                If Sort IsNot Nothing AndAlso Sort.IsExternal Then
                    r = CType(dbm.MappingEngine.ExternalSort(Of ReturnType)(dbm, Sort, values), ReadOnlyObjectList(Of ReturnType))
                Else
                    r = New ReadOnlyObjectList(Of ReturnType)(values)
                End If

                Return r
            End Function

            Public Overrides Function GetCacheItem(ByVal ctx As TypeWrap(Of Object)) As CachedItemBase
                Return New CachedItem(GetEntities(), _mgr.Cache)
            End Function

            Public Overrides Sub Reset(ByVal mgr As OrmManager, ByVal j As List(Of List(Of QueryJoin)), _
                                       ByVal f() As Criteria.Core.IFilter, ByVal sl As System.Collections.Generic.List(Of System.Collections.Generic.List(Of Entities.SelectExpression)), ByVal q As QueryCmd)
                Throw New NotImplementedException
            End Sub
        End Class
    End Class
End Namespace