Imports Worm.Criteria.Core
Imports Worm.Criteria.Conditions
Imports Worm.Entities.Meta
Imports Worm.Entities
Imports System.Collections.Generic
Imports Worm.Query

Namespace Criteria.Joins
    Public Class JoinLink
        Implements IGetFilter

        Private _c As Condition.ConditionConstructor
        Private _jc As List(Of QueryJoin)

        Protected Friend Sub New(ByVal f As IFilter, ByVal jc As List(Of QueryJoin))
            _c = New Condition.ConditionConstructor
            _c.AddFilter(f)
            _jc = jc
        End Sub

        Protected Friend Sub New(ByVal t As Type, ByVal jc As List(Of QueryJoin))
            _jc = jc
            AddType(t)
        End Sub

        Protected Friend Sub New(ByVal entityName As String, ByVal jc As List(Of QueryJoin))
            _jc = jc
            AddType(entityName)
        End Sub

        Protected Friend Sub New(ByVal t As Type, ByVal key As String, ByVal jc As List(Of QueryJoin))
            _jc = jc
            AddType(t, key)
        End Sub

        Protected Friend Sub New(ByVal entityName As String, ByVal key As String, ByVal jc As List(Of QueryJoin))
            _jc = jc
            AddType(entityName, key)
        End Sub

        Protected Friend Sub New(ByVal c As Condition.ConditionConstructor, ByVal jc As List(Of QueryJoin))
            _c = c
            _jc = jc
        End Sub

        Public Function [and](ByVal f As IGetFilter) As JoinLink
            _c.AddFilter(f.Filter, Worm.Criteria.Conditions.ConditionOperator.And)
            Return Me
        End Function

        Public Function [and](ByVal t As Type, ByVal propertyAlias As String) As CriteriaJoin
            Dim jf As New JoinFilter(t, propertyAlias, CType(Nothing, Type), Nothing, FilterOperation.Equal)
            Dim c As New CriteriaJoin(jf, _c, _jc)
            Return c
        End Function

        Public Function [and](ByVal table As SourceFragment, ByVal column As String) As CriteriaJoin
            Dim jf As New JoinFilter(table, column, CType(Nothing, Type), Nothing, FilterOperation.Equal)
            Dim c As New CriteriaJoin(jf, _c, _jc)
            Return c
        End Function

        'Public ReadOnly Property Condition() As IFilter
        '    Get
        '        Return _c.Condition
        '    End Get
        'End Property

        'Protected ReadOnly Property JC() As JCtor
        '    Get
        '        Return _jc
        '    End Get
        'End Property

        Protected Friend Sub AddFilter(ByVal jf As IFilter)
            _jc(_jc.Count - 1).Condition = jf
        End Sub

        Protected Friend Sub AddType(ByVal t As Type)
            _jc(_jc.Count - 1).M2MJoinType = t
        End Sub

        Protected Friend Sub AddType(ByVal t As Type, ByVal key As String)
            _jc(_jc.Count - 1).M2MJoinType = t
            _jc(_jc.Count - 1).M2MKey = key
        End Sub

        Protected Friend Sub AddType(ByVal entityName As String)
            _jc(_jc.Count - 1).M2MJoinEntityName = entityName
        End Sub

        Protected Friend Sub AddType(ByVal entityName As String, ByVal key As String)
            _jc(_jc.Count - 1).M2MJoinEntityName = entityName
            _jc(_jc.Count - 1).M2MKey = key
        End Sub

        Public Function join(ByVal t As Type) As JoinCondition
            AddFilter(_c.Condition)
            Dim j As New QueryJoin(t, Worm.Criteria.Joins.JoinType.Join, CType(Nothing, IFilter))
            _jc.Add(j)
            Return New JoinCondition(_jc)
        End Function

        Public Function join(ByVal entityName As String) As JoinCondition
            AddFilter(_c.Condition)
            Dim j As New QueryJoin(entityName, Worm.Criteria.Joins.JoinType.Join, Nothing)
            _jc.Add(j)
            Return New JoinCondition(_jc)
        End Function

        Public Function left_join(ByVal t As Type) As JoinCondition
            AddFilter(_c.Condition)
            Dim j As New QueryJoin(t, Worm.Criteria.Joins.JoinType.LeftOuterJoin, CType(Nothing, IFilter))
            _jc.Add(j)
            Return New JoinCondition(_jc)
        End Function

        Public Function left_join(ByVal entityName As String) As JoinCondition
            AddFilter(_c.Condition)
            Dim j As New QueryJoin(entityName, Worm.Criteria.Joins.JoinType.LeftOuterJoin, Nothing)
            _jc.Add(j)
            Return New JoinCondition(_jc)
        End Function

        Public Function right_join(ByVal t As Type) As JoinCondition
            AddFilter(_c.Condition)
            Dim j As New QueryJoin(t, Worm.Criteria.Joins.JoinType.RightOuterJoin, CType(Nothing, IFilter))
            _jc.Add(j)
            Return New JoinCondition(_jc)
        End Function

        Public Function right_join(ByVal entityName As String) As JoinCondition
            AddFilter(_c.Condition)
            Dim j As New QueryJoin(entityName, Worm.Criteria.Joins.JoinType.RightOuterJoin, Nothing)
            _jc.Add(j)
            Return New JoinCondition(_jc)
        End Function

        Public Function Join(ByVal table As SourceFragment) As JoinCondition
            AddFilter(_c.Condition)
            Dim j As New QueryJoin(table, Worm.Criteria.Joins.JoinType.Join, Nothing)
            _jc.Add(j)
            Return New JoinCondition(_jc)
        End Function

        Public Function left_join(ByVal table As SourceFragment) As JoinCondition
            AddFilter(_c.Condition)
            Dim j As New QueryJoin(table, Worm.Criteria.Joins.JoinType.LeftOuterJoin, Nothing)
            _jc.Add(j)
            Return New JoinCondition(_jc)
        End Function

        Public Function right_join(ByVal table As SourceFragment) As JoinCondition
            AddFilter(_c.Condition)
            Dim j As New QueryJoin(table, Worm.Criteria.Joins.JoinType.RightOuterJoin, Nothing)
            _jc.Add(j)
            Return New JoinCondition(_jc)
        End Function

        Public Shared Widening Operator CType(ByVal jl As JoinLink) As QueryJoin()
            If jl._c IsNot Nothing Then
                jl.AddFilter(jl._c.Condition)
            End If
            Return jl._jc.ToArray
        End Operator

        Public ReadOnly Property Filter() As IFilter Implements IGetFilter.Filter
            Get
                Return _c.Condition
            End Get
        End Property

        'Public ReadOnly Property Filter(ByVal t As System.Type) As IFilter Implements IGetFilter.Filter
        '    Get
        '        Return _c.Condition
        '    End Get
        'End Property
    End Class

    Public Class CriteriaJoin
        Private _jf As JoinFilter
        Private _c As Condition.ConditionConstructor
        Private _jc As List(Of QueryJoin)

        Protected Friend Sub New(ByVal jf As JoinFilter, ByVal jc As List(Of QueryJoin))
            _jf = jf
            _jc = jc
        End Sub

        Protected Friend Sub New(ByVal jf As JoinFilter, ByVal c As Condition.ConditionConstructor, ByVal jc As List(Of QueryJoin))
            _jf = jf
            _c = c
            _jc = jc
        End Sub

        Public Function eq(ByVal t As Type, ByVal propertyAlias As String) As JoinLink
            _jf.Right = New FieldReference(New ObjectSource(t), propertyAlias)
            _jf._oper = FilterOperation.Equal
            Return GetLink()
        End Function

        Public Function eq(ByVal entityName As String, ByVal propertyAlias As String) As JoinLink
            _jf.Right = New FieldReference(New ObjectSource(entityName), propertyAlias)
            _jf._oper = FilterOperation.Equal
            Return GetLink()
        End Function

        Public Function eq(ByVal [alias] As ObjectAlias, ByVal propertyAlias As String) As JoinLink
            _jf.Right = New FieldReference(New ObjectSource([alias]), propertyAlias)
            _jf._oper = FilterOperation.Equal
            Return GetLink()
        End Function

        Public Function eq(ByVal table As SourceFragment, ByVal column As String) As JoinLink
            _jf.Right = New FieldReference(table, column)
            _jf._oper = FilterOperation.Equal
            Return GetLink()
        End Function

        Protected Function GetLink() As JoinLink
            If _c Is Nothing Then
                _c = New Condition.ConditionConstructor
            End If
            _c.AddFilter(_jf)
            Return New JoinLink(_c, _jc)
        End Function
    End Class

End Namespace