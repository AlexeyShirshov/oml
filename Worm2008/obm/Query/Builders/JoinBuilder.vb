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

#Region " Relations "
        Public Shared Function join_relation(ByVal r As RelationDesc) As JC2
            Return New JC2(r)
        End Function

        Class JC2
            Private _r As RelationDesc
            Private _j As List(Of QueryJoin)

            Public Sub New(ByVal r As RelationDesc)
                _r = r
            End Sub

            Public Sub New(ByVal r As RelationDesc, ByVal j As List(Of QueryJoin))
                _r = r
                _j = j
            End Sub

            Public Function [with](ByVal t As Type) As JC3
                Return CreateJC(GetJoin(t))
            End Function

            Public Function [with](ByVal en As String) As JC3
                Return CreateJC(GetJoin(en))
            End Function

            Public Function [with](ByVal eu As EntityUnion) As JC3
                Return CreateJC(GetJoin(eu))
            End Function

            Public Function [with](ByVal ea As EntityAlias) As JC3
                Return CreateJC(GetJoin(ea))
            End Function

            Protected Function GetJoin(ByVal t As Type) As QueryJoin
                If GetType(M2MRelationDesc).IsAssignableFrom(_r.GetType) Then
                    Return JCtor.join(_r.Rel).onM2M(_r.Key, t)
                Else
                    Return JCtor.join(_r.Rel).on(_r.Rel, ObjectProperty.PrimaryKeyReference).eq(t, _r.Column)
                End If
            End Function

            Protected Function GetJoin(ByVal en As String) As QueryJoin
                If GetType(M2MRelationDesc).IsAssignableFrom(_r.GetType) Then
                    Return JCtor.join(_r.Rel).onM2M(_r.Key, en)
                Else
                    Return JCtor.join(_r.Rel).on(_r.Rel, ObjectProperty.PrimaryKeyReference).eq(en, _r.Column)
                End If
            End Function

            Protected Function GetJoin(ByVal eu As EntityUnion) As QueryJoin
                If GetType(M2MRelationDesc).IsAssignableFrom(_r.GetType) Then
                    Return JCtor.join(_r.Rel).onM2M(_r.Key, eu)
                Else
                    Return JCtor.join(_r.Rel).on(_r.Rel, ObjectProperty.PrimaryKeyReference).eq(eu, _r.Column)
                End If
            End Function

            Protected Function GetJoin(ByVal ea As EntityAlias) As QueryJoin
                If GetType(M2MRelationDesc).IsAssignableFrom(_r.GetType) Then
                    Return JCtor.join(_r.Rel).onM2M(_r.Key, ea)
                Else
                    Return JCtor.join(_r.Rel).on(_r.Rel, ObjectProperty.PrimaryKeyReference).eq(ea, _r.Column)
                End If
            End Function

            Protected Function CreateJC(ByVal j As QueryJoin) As JC3
                If _j Is Nothing Then
                    _j = New List(Of QueryJoin)
                End If
                _j.Add(j)
                Return New JC3(_j)
            End Function
        End Class

        Class JoinLinkBase
            Protected _jc As List(Of QueryJoin)

            Public Sub New(ByVal l As List(Of QueryJoin))
                _jc = l
            End Sub

            Protected Overridable Sub PreAdd()

            End Sub

            Public Function join(ByVal ea As EntityAlias) As JoinCondition
                PreAdd()
                Dim j As New QueryJoin(ea, Worm.Criteria.Joins.JoinType.Join, CType(Nothing, IFilter))
                _jc.Add(j)
                Return New JoinCondition(_jc)
            End Function

            Public Function join(ByVal eu As EntityUnion) As JoinCondition
                PreAdd()
                Dim j As New QueryJoin(eu, Worm.Criteria.Joins.JoinType.Join, CType(Nothing, IFilter))
                _jc.Add(j)
                Return New JoinCondition(_jc)
            End Function

            Public Function join(ByVal t As Type) As JoinCondition
                PreAdd()
                Dim j As New QueryJoin(t, Worm.Criteria.Joins.JoinType.Join, CType(Nothing, IFilter))
                _jc.Add(j)
                Return New JoinCondition(_jc)
            End Function

            Public Function join(ByVal entityName As String) As JoinCondition
                PreAdd()
                Dim j As New QueryJoin(entityName, Worm.Criteria.Joins.JoinType.Join, Nothing)
                _jc.Add(j)
                Return New JoinCondition(_jc)
            End Function

            Public Function left_join(ByVal eu As EntityUnion) As JoinCondition
                PreAdd()
                Dim j As New QueryJoin(eu, Worm.Criteria.Joins.JoinType.LeftOuterJoin, CType(Nothing, IFilter))
                _jc.Add(j)
                Return New JoinCondition(_jc)
            End Function

            Public Function left_join(ByVal t As Type) As JoinCondition
                PreAdd()
                Dim j As New QueryJoin(t, Worm.Criteria.Joins.JoinType.LeftOuterJoin, CType(Nothing, IFilter))
                _jc.Add(j)
                Return New JoinCondition(_jc)
            End Function

            Public Function left_join(ByVal entityName As String) As JoinCondition
                PreAdd()
                Dim j As New QueryJoin(entityName, Worm.Criteria.Joins.JoinType.LeftOuterJoin, Nothing)
                _jc.Add(j)
                Return New JoinCondition(_jc)
            End Function

            Public Function right_join(ByVal eu As EntityUnion) As JoinCondition
                PreAdd()
                Dim j As New QueryJoin(eu, Worm.Criteria.Joins.JoinType.RightOuterJoin, CType(Nothing, IFilter))
                _jc.Add(j)
                Return New JoinCondition(_jc)
            End Function

            Public Function right_join(ByVal t As Type) As JoinCondition
                PreAdd()
                Dim j As New QueryJoin(t, Worm.Criteria.Joins.JoinType.RightOuterJoin, CType(Nothing, IFilter))
                _jc.Add(j)
                Return New JoinCondition(_jc)
            End Function

            Public Function right_join(ByVal entityName As String) As JoinCondition
                PreAdd()
                Dim j As New QueryJoin(entityName, Worm.Criteria.Joins.JoinType.RightOuterJoin, Nothing)
                _jc.Add(j)
                Return New JoinCondition(_jc)
            End Function

            Public Function Join(ByVal table As SourceFragment) As JoinCondition
                PreAdd()
                Dim j As New QueryJoin(table, Worm.Criteria.Joins.JoinType.Join, Nothing)
                _jc.Add(j)
                Return New JoinCondition(_jc)
            End Function

            Public Function left_join(ByVal table As SourceFragment) As JoinCondition
                PreAdd()
                Dim j As New QueryJoin(table, Worm.Criteria.Joins.JoinType.LeftOuterJoin, Nothing)
                _jc.Add(j)
                Return New JoinCondition(_jc)
            End Function

            Public Function right_join(ByVal table As SourceFragment) As JoinCondition
                PreAdd()
                Dim j As New QueryJoin(table, Worm.Criteria.Joins.JoinType.RightOuterJoin, Nothing)
                _jc.Add(j)
                Return New JoinCondition(_jc)
            End Function
        End Class

        Class JC3
            Inherits JoinLinkBase
            'Private _j As List(Of QueryJoin)

            Public Sub New(ByVal l As List(Of QueryJoin))
                MyBase.New(l)
            End Sub

            Public Function join_relation(ByVal r As RelationDesc) As JC2
                Return New JC2(r, _jc)
            End Function

            Public Shared Widening Operator CType(ByVal jl As JC3) As QueryJoin()
                'jl.AddFilterCon()
                Return jl._jc.ToArray
            End Operator
        End Class
#End Region
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

        Public Function onM2M(ByVal m2mKey As String, ByVal m2mEU As EntityUnion) As JoinLink
            Return New JoinLink(m2mEU, m2mKey, _j)
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

        Public Function [on](ByVal eu As EntityUnion, ByVal propertyAlias As String) As CriteriaJoin
            Dim jf As New JoinFilter(eu, propertyAlias, CType(Nothing, Type), Nothing, FilterOperation.Equal)
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