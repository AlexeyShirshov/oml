Imports Worm.Entities
Imports Worm.Query.Sorting
Imports Worm.Entities.Meta
Imports Worm.Criteria.Values
Imports Worm.Expressions2

Namespace Query
    'Public Class SCtorOld
    '    'Private _os As EntityUnion
    '    'Private _prev As SortLink

    '    'Protected Friend Sub New(ByVal os As EntityUnion, ByVal prev As SortLink)
    '    '    _os = os
    '    '    _prev = prev
    '    'End Sub

    '    'Public Function next_prop(ByVal propertyAlias As String) As SortLink
    '    '    Return New SortLink(_os, propertyAlias, _prev)
    '    'End Function

    '    'Public Function NextExternal(ByVal propertyAlias As String) As SortLink
    '    '    Return New SortLink(_os, propertyAlias, True, _prev)
    '    'End Function

    '    Public Shared Function prop(ByVal en As String, ByVal propertyAlias As String) As SortLink
    '        Return New SortLink(New EntityUnion(en), propertyAlias)
    '    End Function

    '    Public Shared Function prop(ByVal propertyAlias As String) As SortLink
    '        Return New SortLink(CType(Nothing, EntityUnion), propertyAlias)
    '    End Function

    '    Public Shared Function prop(ByVal [alias] As QueryAlias, ByVal propertyAlias As String) As SortLink
    '        Return New SortLink(New EntityUnion([alias]), propertyAlias)
    '    End Function

    '    Public Shared Function column(ByVal t As SourceFragment, ByVal clm As String) As SortLink
    '        Return New SortLink(t, clm)
    '    End Function

    '    Public Shared Function query(ByVal queryCmd As QueryCmd) As SortLink
    '        Return New SortLink(queryCmd)
    '    End Function

    '    'Public Shared Function Custom(ByVal sortExpression As String, ByVal values() As Pair(Of Object, String)) As SortOrder
    '    '    Return SortOrder.CreateCustom(sortExpression, Nothing, values)
    '    'End Function

    '    'Public Shared Function Custom(ByVal sortExpression As String) As SortLink
    '    '    Return SortLink.CreateCustom(sortExpression, Nothing, Nothing)
    '    'End Function

    '    Public Shared Function custom(ByVal sortExpression As String, ByVal ParamArray values() As IFilterValue) As SortLink
    '        Return New SortLink(sortExpression, values)
    '    End Function

    '    Public Shared Function custom(ByVal sortExpression As String, ByVal values() As SelectExpression) As SortLink
    '        Return New SortLink(sortExpression, Array.ConvertAll(values, Function(se As SelectExpression) New SelectExpressionValue(se)))
    '    End Function

    '    Public Shared Function Exp(ByVal se As SelectExpression) As SortLink
    '        Return New SortLink(se)
    '    End Function

    '    Public Shared Function prop(ByVal t As Type, ByVal propertyAlias As String) As SortLink
    '        Return New SortLink(New EntityUnion(t), propertyAlias)
    '    End Function

    '    Public Shared Function prop(ByVal os As EntityUnion, ByVal propertyAlias As String) As SortLink
    '        Return New SortLink(os, propertyAlias)
    '    End Function

    '    Public Shared Function prop(ByVal oprop As ObjectProperty) As SortLink
    '        Return New SortLink(oprop.Entity, oprop.PropertyAlias)
    '    End Function

    '    Public Shared Function External(ByVal tag As String, ByVal externalSort As ExternalSortDelegate) As SortLink
    '        Return New SortLink(tag, externalSort)
    '    End Function

    '    Public Shared Function External(ByVal tag As String) As SortLink
    '        Return New SortLink(tag)
    '    End Function

    '    Public Shared Function count() As SortLink
    '        Return New SortLink(FCtor.count)
    '    End Function

    '    Public Shared Function max(ByVal op As ObjectProperty) As SortLink
    '        Return New SortLink(FCtor.max(op))
    '    End Function

    '    Public Shared Function min(ByVal op As ObjectProperty) As SortLink
    '        Return New SortLink(FCtor.min(op))
    '    End Function

    '    Public Shared Function sum(ByVal op As ObjectProperty) As SortLink
    '        Return New SortLink(FCtor.sum(op))
    '    End Function

    '    Public Shared Function sum(ByVal prop As String) As SortLink
    '        Return New SortLink(FCtor.sum(prop))
    '    End Function

    '    Public Class SortLink
    '        Private _s As Sort

    '        Public Sub New(ByVal s As Sort)
    '            _s = s
    '        End Sub

    '        Public Sub New(ByVal eu As EntityUnion, ByVal propertyAlias As String)
    '            _s = New Sort(eu, propertyAlias, SortType.Asc, False, Nothing)
    '        End Sub

    '        Public Sub New(ByVal tbl As SourceFragment, ByVal column As String)
    '            _s = New Sort(tbl, column, SortType.Asc, False)
    '        End Sub

    '        Public Sub New(ByVal format As String, ByVal params() As IFilterValue)
    '            _s = New Sort(format, params)
    '        End Sub

    '        Public Sub New(ByVal q As QueryCmd)
    '            _s = New Sort(q)
    '        End Sub

    '        Public Sub New(ByVal tag As String)
    '            _s = New Sort(CType(Nothing, EntityUnion), Nothing, SortType.Asc, True, Nothing)
    '            _s.Column = tag
    '        End Sub

    '        Public Sub New(ByVal tag As String, ByVal del As ExternalSortDelegate)
    '            _s = New Sort(CType(Nothing, EntityUnion), Nothing, SortType.Asc, True, del)
    '            _s.Column = tag
    '        End Sub

    '        Public Sub New(ByVal se As SelectExpressionOld)
    '            _s = New Sort()
    '            se.CopyTo(_s)
    '        End Sub

    '        Protected Friend Sub New(ByVal prev As Sort, ByVal eu As EntityUnion, ByVal propertyAlias As String)
    '            _s = New Sort(prev, eu, propertyAlias, SortType.Asc, False, Nothing)
    '        End Sub

    '        Protected Friend Sub New(ByVal prev As Sort, ByVal tbl As SourceFragment, ByVal column As String)
    '            _s = New Sort(prev, tbl, column, SortType.Asc, False, Nothing)
    '        End Sub

    '        Protected Friend Sub New(ByVal prev As Sort, ByVal format As String, ByVal params() As IFilterValue)
    '            _s = New Sort(prev, format, params)
    '        End Sub

    '        Protected Friend Sub New(ByVal prev As Sort, ByVal q As QueryCmd)
    '            _s = New Sort(prev, q)
    '        End Sub

    '        Protected Friend Sub New(ByVal prev As Sort, ByVal tag As String)
    '            _s = New Sort(prev, CType(Nothing, EntityUnion), Nothing, SortType.Asc, True, Nothing)
    '            _s.Column = tag
    '        End Sub

    '        Protected Friend Sub New(ByVal prev As Sort, ByVal tag As String, ByVal del As ExternalSortDelegate)
    '            _s = New Sort(prev, CType(Nothing, EntityUnion), Nothing, SortType.Asc, True, del)
    '            _s.Column = tag
    '        End Sub

    '        Protected Friend Sub New(ByVal prev As Sort, ByVal se As SelectExpression)
    '            _s = New Sort(prev)
    '            se.CopyTo(_s)
    '        End Sub

    '        Public Function prop(ByVal t As Type, ByVal propertyAlias As String) As SortLink
    '            Return New SortLink(_s, New EntityUnion(t), propertyAlias)
    '        End Function

    '        Public Function prop(ByVal eu As EntityUnion, ByVal propertyAlias As String) As SortLink
    '            Return New SortLink(_s, eu, propertyAlias)
    '        End Function

    '        Public Function prop(ByVal en As String, ByVal propertyAlias As String) As SortLink
    '            Return New SortLink(_s, New EntityUnion(en), propertyAlias)
    '        End Function

    '        Public Function prop(ByVal propertyAlias As String) As SortLink
    '            Return New SortLink(_s, _s.ObjectSource, propertyAlias)
    '        End Function

    '        Public Function prop(ByVal op As ObjectProperty) As SortLink
    '            Return New SortLink(_s, op.Entity, op.PropertyAlias)
    '        End Function

    '        Public Function column(ByVal clm As String) As SortLink
    '            Return New SortLink(_s, _s.Table, clm)
    '        End Function

    '        Public Function column(ByVal table As SourceFragment, ByVal clm As String) As SortLink
    '            Return New SortLink(_s, table, clm)
    '        End Function

    '        Public Function query(ByVal queryCmd As QueryCmd) As SortLink
    '            Return New SortLink(_s, queryCmd)
    '        End Function

    '        Public Function custom(ByVal sortexpression As String, ByVal ParamArray values() As IFilterValue) As SortLink
    '            Return New SortLink(_s, sortexpression, values)
    '        End Function

    '        Public Function custom(ByVal sortexpression As String, ByVal values() As SelectExpression) As SortLink
    '            Return New SortLink(_s, sortexpression, Array.ConvertAll(values, Function(se As SelectExpression) New SelectExpressionValue(se)))
    '        End Function

    '        Public Function Exp(ByVal se As SelectExpression) As SortLink
    '            Return New SortLink(_s, se)
    '        End Function

    '        Public Function count() As SortLink
    '            Return New SortLink(_s, FCtor.count)
    '        End Function

    '        Public Function max(ByVal op As ObjectProperty) As SortLink
    '            Return New SortLink(_s, FCtor.max(op))
    '        End Function

    '        Public Function min(ByVal op As ObjectProperty) As SortLink
    '            Return New SortLink(_s, FCtor.min(op))
    '        End Function

    '        Public Function sum(ByVal op As ObjectProperty) As SortLink
    '            Return New SortLink(_s, FCtor.sum(op))
    '        End Function

    '        Public Function sum(ByVal prop As String) As SortLink
    '            Return New SortLink(_s, FCtor.sum(prop))
    '        End Function

    '        Public ReadOnly Property asc() As SortLink
    '            Get
    '                _s.Order = SortType.Asc
    '                Return Me
    '            End Get
    '        End Property

    '        Public ReadOnly Property desc() As SortLink
    '            Get
    '                _s.Order = SortType.Desc
    '                Return Me
    '            End Get
    '        End Property

    '        Public Function Order(ByVal o As SortType) As SortLink
    '            _s.Order = o
    '            Return Me
    '        End Function

    '        Public Function Order(ByVal asc As Boolean) As SortLink
    '            If asc Then
    '                Return Me.asc
    '            Else
    '                Return Me.desc
    '            End If
    '        End Function

    '        Public Function Order(ByVal sortType As String) As SortLink
    '            _s.Order = CType([Enum].Parse(GetType(SortType), sortType, True), SortType)
    '            Return Me
    '        End Function

    '        Public Shared Widening Operator CType(ByVal so As SortLink) As Sort
    '            Return so._s
    '        End Operator
    '    End Class
    'End Class

    Public Class SCtor

#Region " Shared "
        Public Shared Function prop(ByVal propertyAlias As String) As Int
            Return prop(New PropertyAliasExpression(propertyAlias))
        End Function

        Public Shared Function prop(ByVal t As Type, ByVal propertyAlias As String) As Int
            Return prop(New EntityUnion(t), propertyAlias)
        End Function

        Public Shared Function prop(ByVal entityName As String, ByVal propertyAlias As String) As Int
            Return prop(New EntityUnion(entityName), propertyAlias)
        End Function

        Public Shared Function prop(ByVal [alias] As QueryAlias, ByVal propertyAlias As String) As Int
            Return prop(New EntityUnion([alias]), propertyAlias)
        End Function

        Public Shared Function prop(ByVal os As EntityUnion, ByVal propertyAlias As String) As Int
            Return prop(New EntityExpression(propertyAlias, os))
        End Function

        Public Shared Function prop(ByVal op As ObjectProperty) As Int
            Return prop(New EntityExpression(op))
        End Function

        Public Shared Function prop(ByVal exp As IGetExpression) As Int
            Dim f As New Int
            f.AddExpression(exp.Expression)
            Return f
        End Function

        Public Shared Function column(ByVal table As SourceFragment, ByVal tableColumn As String) As Int
            Dim f As New Int
            f.AddExpression(New TableExpression(table, tableColumn))
            Return f
        End Function

        Public Shared Function column(ByVal inner As QueryCmd) As Int
            Dim f As New Int
            f.AddExpression(New QueryExpression(inner))
            Return f
        End Function

        Public Shared Function custom(ByVal expression As String) As Int
            Dim f As New Int
            f.AddExpression(New CustomExpression(expression))
            Return f
        End Function

        Public Shared Function custom(ByVal expression As String, ByVal ParamArray params() As IGetExpression) As Int
            Dim f As New Int
            f.AddExpression(New CustomExpression(expression, params))
            Return f
        End Function

        Public Shared Function count() As Int
            Dim f As New Int
            f.AddExpression(New AggregateExpression(AggregateExpression.AggregateFunction.Count))
            Return f
        End Function

        Public Shared Function count_distinct(ByVal tbl As SourceFragment, ByVal col As String) As Int
            Return count_distinct(New TableExpression(tbl, col))
        End Function

        Public Shared Function count_distinct(ByVal op As ObjectProperty) As Int
            Return count_distinct(New EntityExpression(op))
        End Function

        Public Shared Function count_distinct(ByVal exp As IGetExpression) As Int
            Dim f As New Int
            f.AddExpression(New AggregateExpression(AggregateExpression.AggregateFunction.Count, exp) With {.Distinct = True})
            Return f
        End Function

        Public Shared Function sum(ByVal table As SourceFragment, ByVal column As String) As Int
            Return sum(New TableExpression(table, column))
        End Function

        Public Shared Function sum(ByVal op As ObjectProperty) As Int
            Return sum(New EntityExpression(op))
        End Function

        Public Shared Function sum(ByVal exp As IGetExpression) As Int
            Dim f As New Int
            f.AddExpression(New AggregateExpression(AggregateExpression.AggregateFunction.Sum, exp))
            Return f
        End Function

        Public Shared Function max(ByVal exp As IGetExpression) As Int
            Dim f As New Int
            f.AddExpression(New AggregateExpression(AggregateExpression.AggregateFunction.Max, exp))
            Return f
        End Function

        Public Shared Function max(ByVal op As ObjectProperty) As Int
            Return max(New EntityExpression(op))
        End Function

        Public Shared Function max(ByVal tbl As SourceFragment, ByVal column As String) As Int
            Return max(New TableExpression(tbl, column))
        End Function

        Public Shared Function min(ByVal exp As IGetExpression) As Int
            Dim f As New Int
            f.AddExpression(New AggregateExpression(AggregateExpression.AggregateFunction.Min, exp))
            Return f
        End Function

        Public Shared Function min(ByVal op As ObjectProperty) As Int
            Return min(New EntityExpression(op))
        End Function

        Public Shared Function min(ByVal tbl As SourceFragment, ByVal column As String) As Int
            Return min(New TableExpression(tbl, column))
        End Function

        Public Shared Function avg(ByVal tbl As SourceFragment, ByVal column As String) As Int
            Return avg(New TableExpression(tbl, column))
        End Function

        Public Shared Function avg(ByVal op As ObjectProperty) As Int
            Return avg(New EntityExpression(op))
        End Function

        Public Shared Function avg(ByVal exp As IGetExpression) As Int
            Dim f As New Int
            f.AddExpression(New AggregateExpression(AggregateExpression.AggregateFunction.Average, exp))
            Return f
        End Function

        Public Shared Function query(ByVal q As QueryCmd) As Int
            Dim f As New Int
            f.AddExpression(New QueryExpression(q))
            Return f
        End Function

        Public Shared Function Exp(ByVal expression As IGetExpression) As Int
            Dim f As New Int
            If expression IsNot Nothing Then
                f.AddExpression(expression.Expression)
            End If
            Return f
        End Function
#End Region

        Class Int
            Inherits ExpCtor(Of Int).Int

            Public ReadOnly Property asc() As Int
                Get
                    If _l IsNot Nothing AndAlso _l.Count > 0 Then
                        CType(_l(_l.Count - 1), SortExpression).Order = SortExpression.SortType.Asc
                    End If
                    Return Me
                End Get
            End Property

            Public ReadOnly Property desc() As Int
                Get
                    If _l IsNot Nothing AndAlso _l.Count > 0 Then
                        CType(_l(_l.Count - 1), SortExpression).Order = SortExpression.SortType.Desc
                    End If
                    Return Me
                End Get
            End Property

            Public Function Order(ByVal o As SortExpression.SortType) As Int
                If _l IsNot Nothing AndAlso _l.Count > 0 Then
                    CType(_l(_l.Count - 1), SortExpression).Order = o
                End If
                Return Me
            End Function

            Public Function Order(ByVal asc As Boolean) As Int
                If asc Then
                    Return Me.asc
                Else
                    Return Me.desc
                End If
            End Function

            Public Function Order(ByVal sortType As String) As Int
                If _l IsNot Nothing AndAlso _l.Count > 0 Then
                    CType(_l(_l.Count - 1), SortExpression).Order = CType([Enum].Parse(GetType(SortExpression.SortType), sortType, True), SortExpression.SortType)
                End If
                Return Me
            End Function

            Protected Overrides Function Wrap(ByVal exp As Expressions2.IExpression) As Expressions2.IExpression
                If TypeOf exp Is SortExpression Then
                    Return exp
                Else
                    Return New SortExpression(exp)
                End If
            End Function

            Public Overloads Shared Widening Operator CType(ByVal so As Int) As SortExpression
                Return CType(so.GetExpressions(0), SortExpression)
            End Operator

            Public Overloads Shared Widening Operator CType(ByVal so As Int) As OrderByClause
                Return New OrderByClause(so.GetExpressions().ConvertAll(Function(e) CType(e, SortExpression)))
            End Operator
        End Class
    End Class

End Namespace