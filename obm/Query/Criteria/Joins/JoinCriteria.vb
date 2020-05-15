Imports Worm.Criteria.Core
Imports Worm.Criteria.Conditions
Imports Worm.Entities.Meta
Imports Worm.Entities
Imports System.Collections.Generic
Imports Worm.Query

Namespace Criteria.Joins

    <Serializable()> _
    Public Class JoinLink
        Inherits JCtor.JoinLinkBase
        Implements IGetFilter

        Private _c As Condition.ConditionConstructor
        'Private _jc As List(Of QueryJoin)

        Protected Friend Sub New(ByVal f As IGetFilter, ByVal jc As List(Of QueryJoin))
            MyBase.New(jc)
            _c = New Condition.ConditionConstructor
            If f IsNot Nothing Then
                _c.AddFilter(f.Filter)
            End If
        End Sub

        Protected Friend Sub New(ByVal os As EntityUnion, ByVal jc As List(Of QueryJoin))
            MyBase.New(jc)
            AddType(os)
        End Sub

        Protected Friend Sub New(ByVal os As EntityUnion, ByVal key As String, ByVal jc As List(Of QueryJoin))
            MyBase.New(jc)
            AddType(os, key)
        End Sub

        Protected Friend Sub New(ByVal t As Type, ByVal jc As List(Of QueryJoin))
            MyBase.New(jc)
            AddType(t)
        End Sub

        Protected Friend Sub New(ByVal entityName As String, ByVal jc As List(Of QueryJoin))
            MyBase.New(jc)
            AddType(entityName)
        End Sub

        Protected Friend Sub New(ByVal t As Type, ByVal key As String, ByVal jc As List(Of QueryJoin))
            MyBase.New(jc)
            AddType(t, key)
        End Sub

        Protected Friend Sub New(ByVal entityName As String, ByVal key As String, ByVal jc As List(Of QueryJoin))
            MyBase.New(jc)
            AddType(entityName, key)
        End Sub

        Protected Friend Sub New(ByVal c As Condition.ConditionConstructor, ByVal jc As List(Of QueryJoin))
            MyBase.New(jc)
            _c = c
        End Sub

#Region " and "
        Public Function [and](ByVal f As IGetFilter) As JoinLink
            _c.AddFilter(f.Filter, Worm.Criteria.Conditions.ConditionOperator.And)
            Return Me
        End Function

        Public Function [and](ByVal t As Type, ByVal propertyAlias As String) As CriteriaJoin
            Dim jf As New JoinFilter(t, propertyAlias, CType(Nothing, Type), Nothing, FilterOperation.Equal)
            Dim c As New CriteriaJoin(jf, _c, _jc)
            Return c
        End Function
        Public Function [and](ByVal table As SourceFragment, ByVal columns As IEnumerable(Of ColumnPair)) As CriteriaJoin
            Dim jf As New JoinFilter(table, columns, CType(Nothing, Type), Nothing, FilterOperation.Equal)
            Dim c As New CriteriaJoin(jf, _c, _jc)
            Return c
        End Function
        Public Function [and](ByVal table As SourceFragment, ByVal column As String) As CriteriaJoin
            Dim jf As New JoinFilter(table, column, CType(Nothing, Type), Nothing, FilterOperation.Equal)
            Dim c As New CriteriaJoin(jf, _c, _jc)
            Return c
        End Function

        Public Function [and](ByVal op As ObjectProperty) As CriteriaJoin
            Dim jf As New JoinFilter(op, CType(Nothing, Type), Nothing, FilterOperation.Equal)
            Dim c As New CriteriaJoin(jf, _c, _jc)
            Return c
        End Function

        Public Function [and](ByVal al As QueryAlias, ByVal propertyAlias As String) As CriteriaJoin
            Dim jf As New JoinFilter(New EntityUnion(al), propertyAlias, CType(Nothing, Type), Nothing, FilterOperation.Equal)
            Dim c As New CriteriaJoin(jf, _c, _jc)
            Return c
        End Function

        Public Function [and](ByVal eu As EntityUnion, ByVal propertyAlias As String) As CriteriaJoin
            Dim jf As New JoinFilter(eu, propertyAlias, CType(Nothing, Type), Nothing, FilterOperation.Equal)
            Dim c As New CriteriaJoin(jf, _c, _jc)
            Return c
        End Function
#End Region

#Region " or "
        Public Function [or](ByVal f As IGetFilter) As JoinLink
            _c.AddFilter(f.Filter, Worm.Criteria.Conditions.ConditionOperator.Or)
            Return Me
        End Function

        Public Function [or](ByVal t As Type, ByVal propertyAlias As String) As CriteriaJoin
            Dim jf As New JoinFilter(t, propertyAlias, CType(Nothing, Type), Nothing, FilterOperation.Equal)
            Dim c As New CriteriaJoin(jf, _c, _jc, ConditionOperator.Or)
            Return c
        End Function

        Public Function [or](ByVal table As SourceFragment, ByVal column As String) As CriteriaJoin
            Dim jf As New JoinFilter(table, column, CType(Nothing, Type), Nothing, FilterOperation.Equal)
            Dim c As New CriteriaJoin(jf, _c, _jc, ConditionOperator.Or)
            Return c
        End Function

        Public Function [or](ByVal op As ObjectProperty) As CriteriaJoin
            Dim jf As New JoinFilter(op, CType(Nothing, Type), Nothing, FilterOperation.Equal)
            Dim c As New CriteriaJoin(jf, _c, _jc, ConditionOperator.Or)
            Return c
        End Function

        Public Function [or](ByVal al As QueryAlias, ByVal propertyAlias As String) As CriteriaJoin
            Dim jf As New JoinFilter(New EntityUnion(al), propertyAlias, CType(Nothing, Type), Nothing, FilterOperation.Equal)
            Dim c As New CriteriaJoin(jf, _c, _jc, ConditionOperator.Or)
            Return c
        End Function

        Public Function [or](ByVal eu As EntityUnion, ByVal propertyAlias As String) As CriteriaJoin
            Dim jf As New JoinFilter(eu, propertyAlias, CType(Nothing, Type), Nothing, FilterOperation.Equal)
            Dim c As New CriteriaJoin(jf, _c, _jc, ConditionOperator.Or)
            Return c
        End Function
#End Region

        Protected Overrides Sub PreAdd()
            If _c IsNot Nothing Then
                _jc(_jc.Count - 1).Condition = _c.Condition
            End If
        End Sub

        Protected Friend Sub AddType(ByVal os As EntityUnion, ByVal key As String)
            _jc(_jc.Count - 1).M2MObjectSource = os
            _jc(_jc.Count - 1).M2MKey = key
        End Sub

        Protected Friend Sub AddType(ByVal os As EntityUnion)
            _jc(_jc.Count - 1).M2MObjectSource = os
        End Sub

        Protected Friend Sub AddType(ByVal t As Type)
            _jc(_jc.Count - 1).M2MObjectSource = New EntityUnion(t)
        End Sub

        Protected Friend Sub AddType(ByVal t As Type, ByVal key As String)
            _jc(_jc.Count - 1).M2MObjectSource = New EntityUnion(t)
            _jc(_jc.Count - 1).M2MKey = key
        End Sub

        Protected Friend Sub AddType(ByVal entityName As String)
            _jc(_jc.Count - 1).M2MObjectSource = New EntityUnion(entityName)
        End Sub

        Protected Friend Sub AddType(ByVal entityName As String, ByVal key As String)
            _jc(_jc.Count - 1).M2MObjectSource = New EntityUnion(entityName)
            _jc(_jc.Count - 1).M2MKey = key
        End Sub

        'Public Shared Widening Operator CType(ByVal jl As JoinLink) As QueryJoin()
        '    jl.PreAdd()
        '    Return jl._jc.ToArray
        'End Operator

        'Public Shared Widening Operator CType(ByVal jl As JoinLink) As QueryJoin
        '    jl.PreAdd()
        '    Return jl._jc(0)
        'End Operator

        Public Function ToList() As IList(Of QueryJoin)
            PreAdd()
            Return _jc
        End Function

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
        Private _operator As ConditionOperator

        Protected Friend Sub New(ByVal jf As JoinFilter, ByVal jc As List(Of QueryJoin))
            _jf = jf
            _jc = jc
        End Sub
        Protected Friend Sub New(ByVal jf As JoinFilter, ByVal c As Condition.ConditionConstructor, ByVal jc As List(Of QueryJoin))
            MyClass.New(jf, c, jc, ConditionOperator.And)
        End Sub
        Protected Friend Sub New(ByVal jf As JoinFilter, ByVal c As Condition.ConditionConstructor, ByVal jc As List(Of QueryJoin), ByVal [operator] As ConditionOperator)
            _jf = jf
            _c = c
            _jc = jc
            _operator = [operator]
        End Sub

#Region " eq "
        Public Function eq(ByVal t As Type, ByVal propertyAlias As String) As JoinLink
            _jf.Right = New FieldReference(New EntityUnion(t), propertyAlias)
            _jf._oper = FilterOperation.Equal
            Return GetLink()
        End Function

        Public Function eq(ByVal entityName As String, ByVal propertyAlias As String) As JoinLink
            _jf.Right = New FieldReference(New EntityUnion(entityName), propertyAlias)
            _jf._oper = FilterOperation.Equal
            Return GetLink()
        End Function

        Public Function eq(ByVal op As ObjectProperty) As JoinLink
            _jf.Right = New FieldReference(op.Entity, op.PropertyAlias)
            _jf._oper = FilterOperation.Equal
            Return GetLink()
        End Function

        Public Function eq(ByVal [alias] As QueryAlias, ByVal propertyAlias As String) As JoinLink
            _jf.Right = New FieldReference(New EntityUnion([alias]), propertyAlias)
            _jf._oper = FilterOperation.Equal
            Return GetLink()
        End Function

        Public Function eq(ByVal eu As EntityUnion, ByVal propertyAlias As String) As JoinLink
            _jf.Right = New FieldReference(eu, propertyAlias)
            _jf._oper = FilterOperation.Equal
            Return GetLink()
        End Function

        Public Function eq(ByVal table As SourceFragment, ByVal column As String) As JoinLink
            _jf.Right = New FieldReference(table, column)
            _jf._oper = FilterOperation.Equal
            Return GetLink()
        End Function
        Public Function eq(ByVal table As SourceFragment, ByVal columns As IEnumerable(Of ColumnPair)) As JoinLink
            _jf.Right = New FieldReference(table, columns)
            _jf._oper = FilterOperation.Equal
            Return GetLink()
        End Function
        Public Function eq(v As Values.IFilterValue) As JoinLink
            _jf.Right = New FieldReference(v)
            _jf._oper = FilterOperation.Equal
            Return GetLink()
        End Function

#End Region

#Region " not eq "
        Public Function not_eq(ByVal t As Type, ByVal propertyAlias As String) As JoinLink
            _jf.Right = New FieldReference(New EntityUnion(t), propertyAlias)
            _jf._oper = FilterOperation.NotEqual
            Return GetLink()
        End Function

        Public Function not_eq(ByVal entityName As String, ByVal propertyAlias As String) As JoinLink
            _jf.Right = New FieldReference(New EntityUnion(entityName), propertyAlias)
            _jf._oper = FilterOperation.NotEqual
            Return GetLink()
        End Function

        Public Function not_eq(ByVal op As ObjectProperty) As JoinLink
            _jf.Right = New FieldReference(op.Entity, op.PropertyAlias)
            _jf._oper = FilterOperation.NotEqual
            Return GetLink()
        End Function

        Public Function not_eq(ByVal [alias] As QueryAlias, ByVal propertyAlias As String) As JoinLink
            _jf.Right = New FieldReference(New EntityUnion([alias]), propertyAlias)
            _jf._oper = FilterOperation.NotEqual
            Return GetLink()
        End Function

        Public Function not_eq(ByVal eu As EntityUnion, ByVal propertyAlias As String) As JoinLink
            _jf.Right = New FieldReference(eu, propertyAlias)
            _jf._oper = FilterOperation.NotEqual
            Return GetLink()
        End Function

        Public Function not_eq(ByVal table As SourceFragment, ByVal column As String) As JoinLink
            _jf.Right = New FieldReference(table, column)
            _jf._oper = FilterOperation.NotEqual
            Return GetLink()
        End Function
        Public Function not_eq(v As Values.IFilterValue) As JoinLink
            _jf.Right = New FieldReference(v)
            _jf._oper = FilterOperation.NotEqual
            Return GetLink()
        End Function
#End Region
        Protected Function GetLink() As JoinLink
            If _c Is Nothing Then
                _c = New Condition.ConditionConstructor
            End If
            _c.AddFilter(_jf, _operator)
            Return New JoinLink(_c, _jc)
        End Function
    End Class

End Namespace