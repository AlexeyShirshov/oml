Imports System.Collections.Generic

Imports Worm.Entities.Meta
Imports Worm.Criteria
Imports Worm.Criteria.Values
Imports Worm.Criteria.Core
Imports Worm.Entities
Imports Worm.Criteria.Conditions
Imports Worm.Criteria.Joins
Imports System.ComponentModel

'Imports Worm.Database.Criteria.Core

Namespace Query
#Disable Warning IDE1006 ' Naming Styles

    Public Class JCtor
        Private ReadOnly _j As New List(Of QueryJoin)

#Region " Inner joins "
        Public Shared Function join(ByVal t As Type, Optional hint As String = Nothing) As JoinCondition
            Dim j As New QueryJoin(t, Worm.Criteria.Joins.JoinType.Join, CType(Nothing, IFilter)) With {.Hint = hint}
            Dim jc As New JCtor
            jc._j.Add(j)
            Return New JoinCondition(jc._j)
        End Function

        Public Shared Function join(ByVal entityName As String, Optional hint As String = Nothing) As JoinCondition
            Dim j As New QueryJoin(entityName, Worm.Criteria.Joins.JoinType.Join, Nothing) With {.Hint = hint}
            Dim jc As New JCtor
            jc._j.Add(j)
            Return New JoinCondition(jc._j)
        End Function

        Public Shared Function join(ByVal [alias] As QueryAlias) As JoinCondition
            Dim j As New QueryJoin([alias], Worm.Criteria.Joins.JoinType.Join, Nothing)
            Dim jc As New JCtor
            jc._j.Add(j)
            Return New JoinCondition(jc._j)
        End Function

        Public Shared Function join(ByVal os As EntityUnion, Optional hint As String = Nothing) As JoinCondition
            Dim j As New QueryJoin(os, Worm.Criteria.Joins.JoinType.Join, Nothing) With {.Hint = hint}
            Dim jc As New JCtor
            jc._j.Add(j)
            Return New JoinCondition(jc._j)
        End Function

        Public Shared Function join(ByVal table As SourceFragment, Optional hint As String = Nothing) As JoinCondition
            Dim j As New QueryJoin(table, Worm.Criteria.Joins.JoinType.Join, Nothing) With {.Hint = hint}
            Dim jc As New JCtor
            jc._j.Add(j)
            Return New JoinCondition(jc._j)
        End Function

#End Region

#Region " Left joins "
        Public Shared Function left_join(ByVal table As SourceFragment, Optional hint As String = Nothing) As JoinCondition
            Dim j As New QueryJoin(table, Worm.Criteria.Joins.JoinType.LeftOuterJoin, Nothing) With {.Hint = hint}
            Dim jc As New JCtor
            jc._j.Add(j)
            Return New JoinCondition(jc._j)
        End Function

        Public Shared Function left_join(ByVal t As Type, Optional hint As String = Nothing) As JoinCondition
            Dim j As New QueryJoin(t, Worm.Criteria.Joins.JoinType.LeftOuterJoin, CType(Nothing, IFilter)) With {.Hint = hint}
            Dim jc As New JCtor
            jc._j.Add(j)
            Return New JoinCondition(jc._j)
        End Function

        Public Shared Function left_join(ByVal entityName As String, Optional hint As String = Nothing) As JoinCondition
            Dim j As New QueryJoin(entityName, Worm.Criteria.Joins.JoinType.LeftOuterJoin, Nothing) With {.Hint = hint}
            Dim jc As New JCtor
            jc._j.Add(j)
            Return New JoinCondition(jc._j)
        End Function

        Public Shared Function left_join(ByVal [alias] As QueryAlias) As JoinCondition
            Dim j As New QueryJoin([alias], Worm.Criteria.Joins.JoinType.LeftOuterJoin, Nothing)
            Dim jc As New JCtor
            jc._j.Add(j)
            Return New JoinCondition(jc._j)
        End Function

        Public Shared Function left_join(ByVal eu As EntityUnion, Optional hint As String = Nothing) As JoinCondition
            Dim j As New QueryJoin(eu, Worm.Criteria.Joins.JoinType.LeftOuterJoin, Nothing) With {.Hint = hint}
            Dim jc As New JCtor
            jc._j.Add(j)
            Return New JoinCondition(jc._j)
        End Function
#End Region

#Region " Right joins "

        Public Shared Function right_join(ByVal t As Type, Optional hint As String = Nothing) As JoinCondition
            Dim j As New QueryJoin(t, Worm.Criteria.Joins.JoinType.RightOuterJoin, CType(Nothing, IFilter)) With {.Hint = hint}
            Dim jc As New JCtor
            jc._j.Add(j)
            Return New JoinCondition(jc._j)
        End Function

        Public Shared Function right_join(ByVal entityName As String, Optional hint As String = Nothing) As JoinCondition
            Dim j As New QueryJoin(entityName, Worm.Criteria.Joins.JoinType.RightOuterJoin, Nothing) With {.Hint = hint}
            Dim jc As New JCtor
            jc._j.Add(j)
            Return New JoinCondition(jc._j)
        End Function

        Public Shared Function right_join(ByVal table As SourceFragment, Optional hint As String = Nothing) As JoinCondition
            Dim j As New QueryJoin(table, Worm.Criteria.Joins.JoinType.RightOuterJoin, Nothing) With {.Hint = hint}
            Dim jc As New JCtor
            jc._j.Add(j)
            Return New JoinCondition(jc._j)
        End Function

#End Region

#Region " Full joins "
        Public Shared Function full_join(ByVal t As Type, Optional hint As String = Nothing) As JoinCondition
            Dim j As New QueryJoin(t, Worm.Criteria.Joins.JoinType.FullJoin, CType(Nothing, IFilter)) With {.Hint = hint}
            Dim jc As New JCtor
            jc._j.Add(j)
            Return New JoinCondition(jc._j)
        End Function

        Public Shared Function full_join(ByVal entityName As String, Optional hint As String = Nothing) As JoinCondition
            Dim j As New QueryJoin(entityName, Worm.Criteria.Joins.JoinType.FullJoin, Nothing) With {.Hint = hint}
            Dim jc As New JCtor
            jc._j.Add(j)
            Return New JoinCondition(jc._j)
        End Function

        Public Shared Function full_join(ByVal [alias] As QueryAlias) As JoinCondition
            Dim j As New QueryJoin([alias], Worm.Criteria.Joins.JoinType.FullJoin, Nothing)
            Dim jc As New JCtor
            jc._j.Add(j)
            Return New JoinCondition(jc._j)
        End Function

        Public Shared Function full_join(ByVal os As EntityUnion, Optional hint As String = Nothing) As JoinCondition
            Dim j As New QueryJoin(os, Worm.Criteria.Joins.JoinType.FullJoin, Nothing) With {.Hint = hint}
            Dim jc As New JCtor
            jc._j.Add(j)
            Return New JoinCondition(jc._j)
        End Function

        Public Shared Function full_join(ByVal table As SourceFragment, Optional hint As String = Nothing) As JoinCondition
            Dim j As New QueryJoin(table, Worm.Criteria.Joins.JoinType.FullJoin, Nothing) With {.Hint = hint}
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
        Public Shared Function join_relation(ByVal eu As EntityUnion, ByVal r As RelationDesc) As JC3
            Return join_relation(New RelationDescEx(eu, r))
        End Function

        Public Shared Function join_relation(ByVal r As RelationDescEx) As JC3
            Return CreateJC(GetJoin(r))
        End Function

        Protected Shared Function GetJoin(ByVal r As RelationDescEx) As QueryJoin
            If GetType(M2MRelationDesc).IsAssignableFrom(r.Rel.GetType) Then
                Return JCtor.join(r.Rel.Entity).onM2M(r.Rel.Key, r.HostEntity)
            Else
                Return JCtor.join(r.Rel.Entity).on(r.HostEntity, ObjectProperty.PrimaryKeyReference).eq(r.Rel.Entity, r.Rel.Column)
            End If
        End Function

        Protected Shared Function CreateJC(ByVal j As QueryJoin) As JC3
            Dim _j As New List(Of QueryJoin) From {
                j
            }
            Return New JC3(_j)
        End Function

        '<EditorBrowsable(EditorBrowsableState.Never)> _
        'Class JC2
        '    Private _r As RelationDescEx
        '    Private _j As List(Of QueryJoin)

        '    Public Sub New(ByVal r As RelationDesc)
        '        _r = r
        '    End Sub

        '    Public Sub New(ByVal r As RelationDesc, ByVal j As List(Of QueryJoin))
        '        _r = r
        '        _j = j
        '    End Sub

        '    Public Function [with](ByVal t As Type) As JC3
        '        Return CreateJC(GetJoin(t))
        '    End Function

        '    Public Function [with](ByVal en As String) As JC3
        '        Return CreateJC(GetJoin(en))
        '    End Function

        '    Public Function [with](ByVal eu As EntityUnion) As JC3
        '        Return CreateJC(GetJoin(eu))
        '    End Function

        '    Public Function [with](ByVal ea As QueryAlias) As JC3
        '        Return CreateJC(GetJoin(ea))
        '    End Function

        '    Protected Function GetJoin(ByVal t As Type) As QueryJoin
        '        If GetType(M2MRelationDesc).IsAssignableFrom(_r.GetType) Then
        '            Return JCtor.join(_r.Rel).onM2M(_r.Key, t)
        '        Else
        '            Return JCtor.join(_r.Rel).on(_r.Rel, ObjectProperty.PrimaryKeyReference).eq(t, _r.Column)
        '        End If
        '    End Function

        '    Protected Function GetJoin(ByVal en As String) As QueryJoin
        '        If GetType(M2MRelationDesc).IsAssignableFrom(_r.GetType) Then
        '            Return JCtor.join(_r.Rel).onM2M(_r.Key, en)
        '        Else
        '            Return JCtor.join(_r.Rel).on(_r.Rel, ObjectProperty.PrimaryKeyReference).eq(en, _r.Column)
        '        End If
        '    End Function

        '    Protected Function GetJoin(ByVal eu As EntityUnion) As QueryJoin
        '        If GetType(M2MRelationDesc).IsAssignableFrom(_r.GetType) Then
        '            Return JCtor.join(_r.Rel).onM2M(_r.Key, eu)
        '        Else
        '            Return JCtor.join(_r.Rel).on(_r.Rel, ObjectProperty.PrimaryKeyReference).eq(eu, _r.Column)
        '        End If
        '    End Function

        '    Protected Function GetJoin(ByVal ea As QueryAlias) As QueryJoin
        '        If GetType(M2MRelationDesc).IsAssignableFrom(_r.GetType) Then
        '            Return JCtor.join(_r.Rel).onM2M(_r.Key, ea)
        '        Else
        '            Return JCtor.join(_r.Rel).on(_r.Rel, ObjectProperty.PrimaryKeyReference).eq(ea, _r.Column)
        '        End If
        '    End Function

        '    Protected Function CreateJC(ByVal j As QueryJoin) As JC3
        '        If _j Is Nothing Then
        '            _j = New List(Of QueryJoin)
        '        End If
        '        _j.Add(j)
        '        Return New JC3(_j)
        '    End Function
        'End Class

        <EditorBrowsable(EditorBrowsableState.Never)> _
        Class JoinLinkBase
            Protected _jc As List(Of QueryJoin)

            Public Sub New(ByVal l As List(Of QueryJoin))
                _jc = l
            End Sub

            Public Sub New(ByVal join As QueryJoin)
                _jc = New List(Of QueryJoin) From {
                    join
                }
            End Sub

            Protected Overridable Sub PreAdd()

            End Sub

            Public Function join(ByVal ea As QueryAlias) As JoinCondition
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

#Region " Apply "

            Public Function apply(ByVal cmd As QueryAlias) As JoinLinkBase
                PreAdd()
                Dim j As New QueryJoin(cmd, Worm.Criteria.Joins.JoinType.InnerApply, CType(Nothing, IFilter))
                _jc.Add(j)
                Return Me
            End Function

            Public Function outer_apply(ByVal cmd As QueryAlias) As JoinLinkBase
                PreAdd()
                Dim j As New QueryJoin(cmd, Worm.Criteria.Joins.JoinType.OuterApply, CType(Nothing, IFilter))
                _jc.Add(j)
                Return Me
            End Function

#End Region

            Public Shared Widening Operator CType(ByVal jl As JoinLinkBase) As QueryJoin()
                jl.PreAdd()
                Return jl._jc.ToArray
            End Operator

            Public Shared Widening Operator CType(ByVal jl As JoinLinkBase) As QueryJoin
                jl.PreAdd()
                Return jl._jc(0)
            End Operator
        End Class

        <EditorBrowsable(EditorBrowsableState.Never)> _
        Class JC3
            Inherits JoinLinkBase
            'Private _j As List(Of QueryJoin)

            Public Sub New(ByVal l As List(Of QueryJoin))
                MyBase.New(l)
            End Sub

            Public Function join_relation(ByVal r As RelationDescEx) As JC3
                _jc.Add(GetJoin(r))
                Return Me
            End Function

            'Public Shared Widening Operator CType(ByVal jl As JC3) As QueryJoin()
            '    'jl.AddFilterCon()
            '    Return jl._jc.ToArray
            'End Operator
        End Class
#End Region

#Region " Apply "

        Public Shared Function apply(ByVal cmd As QueryAlias) As JoinLinkBase
            Dim j As New QueryJoin(cmd, Worm.Criteria.Joins.JoinType.InnerApply, CType(Nothing, IFilter))
            Return New JoinLinkBase(j)
        End Function

        Public Shared Function outer_apply(ByVal cmd As QueryAlias) As JoinLinkBase
            Dim j As New QueryJoin(cmd, Worm.Criteria.Joins.JoinType.OuterApply, CType(Nothing, IFilter))
            Return New JoinLinkBase(j)
        End Function

#End Region

#Region " Cross "
        Public Shared Function cross(ByVal t As Type, Optional hint As String = Nothing) As JoinLinkBase
            Dim j As New QueryJoin(t, Worm.Criteria.Joins.JoinType.CrossJoin, CType(Nothing, IFilter)) With {.Hint = hint}
            Return New JoinLinkBase(j)
        End Function

        Public Shared Function cross(ByVal entityName As String, Optional hint As String = Nothing) As JoinLinkBase
            Dim j As New QueryJoin(entityName, Worm.Criteria.Joins.JoinType.CrossJoin, Nothing) With {.Hint = hint}
            Return New JoinLinkBase(j)
        End Function

        Public Shared Function cross(ByVal [alias] As QueryAlias) As JoinLinkBase
            Dim j As New QueryJoin([alias], Worm.Criteria.Joins.JoinType.CrossJoin, Nothing)
            Return New JoinLinkBase(j)
        End Function

        Public Shared Function cross(ByVal os As EntityUnion, Optional hint As String = Nothing) As JoinLinkBase
            Dim j As New QueryJoin(os, Worm.Criteria.Joins.JoinType.CrossJoin, Nothing) With {.Hint = hint}
            Return New JoinLinkBase(j)
        End Function

        Public Shared Function cross(ByVal table As SourceFragment, Optional hint As String = Nothing) As JoinLinkBase
            Dim j As New QueryJoin(table, Worm.Criteria.Joins.JoinType.CrossJoin, Nothing) With {.Hint = hint}
            Return New JoinLinkBase(j)
        End Function
#End Region
    End Class

    Public Class JoinCondition
        Private ReadOnly _j As List(Of QueryJoin)

        Public Sub New(ByVal l As List(Of QueryJoin))
            _j = l
        End Sub

        Public Function [on](ByVal f As IGetFilter) As JoinLink
            Return New JoinLink(f, _j)
        End Function

        Public Function onM2M(ByVal m2mType As Type) As JoinLink
            Return New JoinLink(m2mType, _j)
        End Function

        Public Function onM2M(ByVal m2mAlias As QueryAlias) As JoinLink
            Return New JoinLink(New EntityUnion(m2mAlias), _j)
        End Function

        Public Function onM2M(ByVal m2mKey As String, ByVal m2mAlias As QueryAlias) As JoinLink
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

        Public Function [on](ByVal [alias] As QueryAlias, ByVal propertyAlias As String) As CriteriaJoin
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
#Enable Warning IDE1006 ' Naming Styles
End Namespace
