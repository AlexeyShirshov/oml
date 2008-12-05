Imports System.Collections.Generic

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

        Public Shared Function join(ByVal [alias] As ObjectAlias) As JoinCondition
            Dim j As New QueryJoin([alias], Worm.Criteria.Joins.JoinType.Join, Nothing)
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

        Public Shared Function join(ByVal table As SourceFragment) As JoinCondition
            Dim j As New QueryJoin(table, Worm.Criteria.Joins.JoinType.Join, Nothing)
            Dim jc As New JCtor
            jc._j.Add(j)
            Return New JoinCondition(jc._j)
        End Function

        Public Shared Function left_join(ByVal table As SourceFragment) As JoinCondition
            Dim j As New QueryJoin(table, Worm.Criteria.Joins.JoinType.LeftOuterJoin, Nothing)
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

        'Public Function AddJoin(ByVal t As Type) As JoinCondition
        '    Dim j As New OrmJoin(t, Worm.Criteria.Joins.JoinType.Join, Nothing)
        '    _j.Add(j)
        '    Return New JoinCondition(Me)
        'End Function

        'Public Function AddLeftJoin(ByVal t As Type) As JoinCondition
        '    Dim j As New OrmJoin(t, Worm.Criteria.Joins.JoinType.LeftOuterJoin, Nothing)
        '    _j.Add(j)
        '    Return New JoinCondition(Me)
        'End Function

        'Public Function AddRightJoin(ByVal t As Type) As JoinCondition
        '    Dim j As New OrmJoin(t, Worm.Criteria.Joins.JoinType.RightOuterJoin, Nothing)
        '    Dim jc As New JCtor
        '    _j.Add(j)
        '    Return New JoinCondition(Me)
        'End Function

        'Public Function AddJoin(ByVal table As SourceFragment) As JoinCondition
        '    Dim j As New OrmJoin(table, Worm.Criteria.Joins.JoinType.Join, Nothing)
        '    Dim jc As New JCtor
        '    _j.Add(j)
        '    Return New JoinCondition(Me)
        'End Function

        'Public Function AddLeftJoin(ByVal table As SourceFragment) As JoinCondition
        '    Dim j As New OrmJoin(table, Worm.Criteria.Joins.JoinType.LeftOuterJoin, Nothing)
        '    Dim jc As New JCtor
        '    _j.Add(j)
        '    Return New JoinCondition(Me)
        'End Function

        'Public Function AddRightJoin(ByVal table As SourceFragment) As JoinCondition
        '    Dim j As New OrmJoin(table, Worm.Criteria.Joins.JoinType.RightOuterJoin, Nothing)
        '    Dim jc As New JCtor
        '    _j.Add(j)
        '    Return New JoinCondition(Me)
        'End Function

        'Protected Friend Function AddFilter(ByVal jf As IFilter) As JCtor
        '    _j(_j.Count - 1).Condition = jf
        '    Return Me
        'End Function

        'Protected Friend Function AddType(ByVal t As Type) As JCtor
        '    _j(_j.Count - 1).M2MJoinType = t
        '    Return Me
        'End Function

        'Protected Friend Function AddType(ByVal t As Type, ByVal key As String) As JCtor
        '    _j(_j.Count - 1).M2MJoinType = t
        '    _j(_j.Count - 1).M2MKey = key
        '    Return Me
        'End Function

        'Protected Friend Function AddType(ByVal entityName As String) As JCtor
        '    _j(_j.Count - 1).M2MJoinEntityName = entityName
        '    Return Me
        'End Function

        'Protected Friend Function AddType(ByVal entityName As String, ByVal key As String) As JCtor
        '    _j(_j.Count - 1).M2MJoinEntityName = entityName
        '    _j(_j.Count - 1).M2MKey = key
        '    Return Me
        'End Function

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

        Public Function onM2M(ByVal m2mEntityName As String) As JoinLink
            Return New JoinLink(m2mEntityName, _j)
        End Function

        Public Function onM2M(ByVal m2mKey As String, ByVal m2mType As Type) As JoinLink
            Return New JoinLink(m2mType, m2mKey, _j)
        End Function

        Public Function onM2M(ByVal m2mKey As String, ByVal m2mEntityName As String) As JoinLink
            Return New JoinLink(m2mEntityName, m2mKey, _j)
        End Function

        Public Function [on](ByVal t As Type, ByVal propertyAlias As String) As CriteriaJoin
            Dim jf As New JoinFilter(t, propertyAlias, CType(Nothing, Type), Nothing, FilterOperation.Equal)
            Dim c As New CriteriaJoin(jf, _j)
            Return c
        End Function

        Public Function [on](ByVal [alias] As ObjectAlias, ByVal propertyAlias As String) As CriteriaJoin
            Dim jf As New JoinFilter(New ObjectSource([alias]), propertyAlias, CType(Nothing, Type), Nothing, FilterOperation.Equal)
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

        Public Shared Function Create(ByVal entityName As String, ByVal propertyAlias As String) As CriteriaJoin
            Dim jf As New JoinFilter(entityName, propertyAlias, CType(Nothing, String), Nothing, FilterOperation.Equal)
            Dim c As New CriteriaJoin(jf, Nothing)
            Return c
        End Function

        Public Shared Function Create(ByVal t As Type, ByVal propertyAlias As String) As CriteriaJoin
            Dim jf As New JoinFilter(t, propertyAlias, CType(Nothing, Type), Nothing, FilterOperation.Equal)
            Dim c As New CriteriaJoin(jf, Nothing)
            Return c
        End Function

        Public Shared Function Create(ByVal table As SourceFragment, ByVal column As String) As CriteriaJoin
            Dim jf As New JoinFilter(table, column, CType(Nothing, Type), Nothing, FilterOperation.Equal)
            Dim c As New CriteriaJoin(jf, Nothing)
            Return c
        End Function
    End Class
End Namespace
