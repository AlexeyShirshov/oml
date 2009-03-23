﻿Imports System.Collections.Generic

Imports Worm.Entities.Meta
Imports Worm.Criteria
Imports Worm.Criteria.Values
Imports Worm.Criteria.Core
Imports Worm.Entities
Imports Worm.Criteria.Conditions
Imports Worm.Criteria.Joins

'Imports Worm.Database.Criteria.Core

Namespace Query

    Public Class JCtor
        Private _j As New List(Of QueryJoin)

#Region " Inner joins "
        Public Shared Function join(ByVal t As Type) As JoinCondition
            Dim j As New QueryJoin(t, Worm.Criteria.Joins.JoinType.Join, CType(Nothing, IFilter))
            Dim jc As New JCtor
            jc._j.Add(j)
            Return New JoinCondition(jc._j)
        End Function

        Public Shared Function join(ByVal entityName As String) As JoinCondition
            Dim j As New QueryJoin(entityName, Worm.Criteria.Joins.JoinType.Join, Nothing)
            Dim jc As New JCtor
            jc._j.Add(j)
            Return New JoinCondition(jc._j)
        End Function

        Public Shared Function join(ByVal [alias] As EntityAlias) As JoinCondition
            Dim j As New QueryJoin([alias], Worm.Criteria.Joins.JoinType.Join, Nothing)
            Dim jc As New JCtor
            jc._j.Add(j)
            Return New JoinCondition(jc._j)
        End Function

        Public Shared Function join(ByVal os As EntityUnion) As JoinCondition
            Dim j As New QueryJoin(os, Worm.Criteria.Joins.JoinType.Join, Nothing)
            Dim jc As New JCtor
            jc._j.Add(j)
            Return New JoinCondition(jc._j)
        End Function

        Public Shared Function join(ByVal table As SourceFragment) As JoinCondition
            Dim j As New QueryJoin(table, Worm.Criteria.Joins.JoinType.Join, Nothing)
            Dim jc As New JCtor
            jc._j.Add(j)
            Return New JoinCondition(jc._j)
        End Function

#End Region

#Region " Left joins "
        Public Shared Function left_join(ByVal table As SourceFragment) As JoinCondition
            Dim j As New QueryJoin(table, Worm.Criteria.Joins.JoinType.LeftOuterJoin, Nothing)
            Dim jc As New JCtor
            jc._j.Add(j)
            Return New JoinCondition(jc._j)
        End Function

        Public Shared Function left_join(ByVal t As Type) As JoinCondition
            Dim j As New QueryJoin(t, Worm.Criteria.Joins.JoinType.LeftOuterJoin, CType(Nothing, IFilter))
            Dim jc As New JCtor
            jc._j.Add(j)
            Return New JoinCondition(jc._j)
        End Function

        Public Shared Function left_join(ByVal entityName As String) As JoinCondition
            Dim j As New QueryJoin(entityName, Worm.Criteria.Joins.JoinType.LeftOuterJoin, Nothing)
            Dim jc As New JCtor
            jc._j.Add(j)
            Return New JoinCondition(jc._j)
        End Function

        Public Shared Function left_join(ByVal [alias] As EntityAlias) As JoinCondition
            Dim j As New QueryJoin([alias], Worm.Criteria.Joins.JoinType.LeftOuterJoin, Nothing)
            Dim jc As New JCtor
            jc._j.Add(j)
            Return New JoinCondition(jc._j)
        End Function

#End Region

#Region " Right joins "

        Public Shared Function right_join(ByVal t As Type) As JoinCondition
            Dim j As New QueryJoin(t, Worm.Criteria.Joins.JoinType.RightOuterJoin, CType(Nothing, IFilter))
            Dim jc As New JCtor
            jc._j.Add(j)
            Return New JoinCondition(jc._j)
        End Function

        Public Shared Function right_join(ByVal entityName As String) As JoinCondition
            Dim j As New QueryJoin(entityName, Worm.Criteria.Joins.JoinType.RightOuterJoin, Nothing)
            Dim jc As New JCtor
            jc._j.Add(j)
            Return New JoinCondition(jc._j)
        End Function

        Public Shared Function right_join(ByVal table As SourceFragment) As JoinCondition
            Dim j As New QueryJoin(table, Worm.Criteria.Joins.JoinType.RightOuterJoin, Nothing)
            Dim jc As New JCtor
            jc._j.Add(j)
            Return New JoinCondition(jc._j)
        End Function

#End Region

#Region " Full joins "
        Public Shared Function full_join(ByVal t As Type) As JoinCondition
            Dim j As New QueryJoin(t, Worm.Criteria.Joins.JoinType.FullJoin, CType(Nothing, IFilter))
            Dim jc As New JCtor
            jc._j.Add(j)
            Return New JoinCondition(jc._j)
        End Function

        Public Shared Function full_join(ByVal entityName As String) As JoinCondition
            Dim j As New QueryJoin(entityName, Worm.Criteria.Joins.JoinType.FullJoin, Nothing)
            Dim jc As New JCtor
            jc._j.Add(j)
            Return New JoinCondition(jc._j)
        End Function

        Public Shared Function full_join(ByVal [alias] As EntityAlias) As JoinCondition
            Dim j As New QueryJoin([alias], Worm.Criteria.Joins.JoinType.FullJoin, Nothing)
            Dim jc As New JCtor
            jc._j.Add(j)
            Return New JoinCondition(jc._j)
        End Function

        Public Shared Function full_join(ByVal os As EntityUnion) As JoinCondition
            Dim j As New QueryJoin(os, Worm.Criteria.Joins.JoinType.FullJoin, Nothing)
            Dim jc As New JCtor
            jc._j.Add(j)
            Return New JoinCondition(jc._j)
        End Function

        Public Shared Function full_join(ByVal table As SourceFragment) As JoinCondition
            Dim j As New QueryJoin(table, Worm.Criteria.Joins.JoinType.FullJoin, Nothing)
            Dim jc As New JCtor
            jc._j.Add(j)
            Return New JoinCondition(jc._j)
        End Function

#End Region

        Public Function Joins() As IList(Of QueryJoin)
            Return _j
        End Function

        Public Function ToJoinArray() As QueryJoin()
            Return _j.ToArray
        End Function

        Friend ReadOnly Property JL() As List(Of QueryJoin)
            Get
                Return _j
            End Get
        End Property
    End Class

    Public Class JoinCondition
        Private _j As List(Of QueryJoin)

        Public Sub New(ByVal l As List(Of QueryJoin))
            _j = l
        End Sub

        Public Function [on](ByVal f As IFilter) As JoinLink
            Return New JoinLink(f, _j)
        End Function

        Public Function onM2M(ByVal m2mType As Type) As JoinLink
            Return New JoinLink(m2mType, _j)
        End Function

        Public Function onM2M(ByVal m2mAlias As EntityAlias) As JoinLink
            Return New JoinLink(New EntityUnion(m2mAlias), _j)
        End Function

        Public Function onM2M(ByVal m2mKey As String, ByVal m2mAlias As EntityAlias) As JoinLink
            Return New JoinLink(New EntityUnion(m2mAlias), m2mKey, _j)
        End Function

        Public Function onM2M(ByVal m2mOS As EntityUnion) As JoinLink
            Return New JoinLink(m2mOS, _j)
        End Function

        Public Function onM2M(ByVal m2mEntityName As String) As JoinLink
            Return New JoinLink(m2mEntityName, _j)
        End Function

        Public Function onM2M(ByVal m2mKey As String, ByVal m2mType As Type) As JoinLink
            Return New JoinLink(m2mType, m2mKey, _j)
        End Function

        Public Function onM2M(ByVal m2mKey As String, ByVal m2mEntityName As String) As JoinLink
            Return New JoinLink(m2mEntityName, m2mKey, _j)
        End Function

        Public Function [on](ByVal op As ObjectProperty) As CriteriaJoin
            Dim jf As New JoinFilter(op, CType(Nothing, Type), Nothing, FilterOperation.Equal)
            Dim c As New CriteriaJoin(jf, _j)
            Return c
        End Function

        Public Function [on](ByVal t As Type, ByVal propertyAlias As String) As CriteriaJoin
            Dim jf As New JoinFilter(t, propertyAlias, CType(Nothing, Type), Nothing, FilterOperation.Equal)
            Dim c As New CriteriaJoin(jf, _j)
            Return c
        End Function

        Public Function [on](ByVal [alias] As EntityAlias, ByVal propertyAlias As String) As CriteriaJoin
            Dim jf As New JoinFilter(New EntityUnion([alias]), propertyAlias, CType(Nothing, Type), Nothing, FilterOperation.Equal)
            Dim c As New CriteriaJoin(jf, _j)
            Return c
        End Function

        Public Function [on](ByVal entityName As String, ByVal propertyAlias As String) As CriteriaJoin
            Dim jf As New JoinFilter(entityName, propertyAlias, CType(Nothing, String), Nothing, FilterOperation.Equal)
            Dim c As New CriteriaJoin(jf, _j)
            Return c
        End Function

        Public Function [on](ByVal table As SourceFragment, ByVal column As String) As CriteriaJoin
            Dim jf As New JoinFilter(table, column, CType(Nothing, Type), Nothing, FilterOperation.Equal)
            Dim c As New CriteriaJoin(jf, _j)
            Return c
        End Function

        'Public Shared Function Create(ByVal entityName As String, ByVal propertyAlias As String) As CriteriaJoin
        '    Dim jf As New JoinFilter(entityName, propertyAlias, CType(Nothing, String), Nothing, FilterOperation.Equal)
        '    Dim c As New CriteriaJoin(jf, Nothing)
        '    Return c
        'End Function

        'Public Shared Function Create(ByVal t As Type, ByVal propertyAlias As String) As CriteriaJoin
        '    Dim jf As New JoinFilter(t, propertyAlias, CType(Nothing, Type), Nothing, FilterOperation.Equal)
        '    Dim c As New CriteriaJoin(jf, Nothing)
        '    Return c
        'End Function

        'Public Shared Function Create(ByVal table As SourceFragment, ByVal column As String) As CriteriaJoin
        '    Dim jf As New JoinFilter(table, column, CType(Nothing, Type), Nothing, FilterOperation.Equal)
        '    Dim c As New CriteriaJoin(jf, Nothing)
        '    Return c
        'End Function
    End Class
End Namespace