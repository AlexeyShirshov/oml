﻿Imports Worm.Entities
Imports Worm.Sorting
Imports Worm.Entities.Meta
Imports Worm.Criteria.Values

Namespace Query
    Public Class SCtor
        'Private _os As EntityUnion
        'Private _prev As SortLink

        'Protected Friend Sub New(ByVal os As EntityUnion, ByVal prev As SortLink)
        '    _os = os
        '    _prev = prev
        'End Sub

        'Public Function next_prop(ByVal propertyAlias As String) As SortLink
        '    Return New SortLink(_os, propertyAlias, _prev)
        'End Function

        'Public Function NextExternal(ByVal propertyAlias As String) As SortLink
        '    Return New SortLink(_os, propertyAlias, True, _prev)
        'End Function

        Public Shared Function prop(ByVal en As String, ByVal propertyAlias As String) As SortLink
            Return New SortLink(New EntityUnion(en), propertyAlias)
        End Function

        Public Shared Function prop(ByVal [alias] As EntityAlias, ByVal propertyAlias As String) As SortLink
            Return New SortLink(New EntityUnion([alias]), propertyAlias)
        End Function

        Public Shared Function column(ByVal t As SourceFragment, ByVal clm As String) As SortLink
            Return New SortLink(t, clm)
        End Function

        Public Shared Function query(ByVal queryCmd As QueryCmd) As SortLink
            Return New SortLink(queryCmd)
        End Function

        'Public Shared Function Custom(ByVal sortExpression As String, ByVal values() As Pair(Of Object, String)) As SortOrder
        '    Return SortOrder.CreateCustom(sortExpression, Nothing, values)
        'End Function

        'Public Shared Function Custom(ByVal sortExpression As String) As SortLink
        '    Return SortLink.CreateCustom(sortExpression, Nothing, Nothing)
        'End Function

        Public Shared Function custom(ByVal sortExpression As String, ByVal ParamArray values() As IFilterValue) As SortLink
            Return New SortLink(sortExpression, values)
        End Function

        Public Shared Function custom(ByVal sortExpression As String, ByVal values() As SelectExpression) As SortLink
            Return New SortLink(sortExpression, Array.ConvertAll(values, Function(se As SelectExpression) New SelectExpressionValue(se)))
        End Function

        Public Shared Function Exp(ByVal se As SelectExpression) As SortLink
            Return New SortLink(se)
        End Function

        Public Shared Function prop(ByVal t As Type, ByVal propertyAlias As String) As SortLink
            Return New SortLink(New EntityUnion(t), propertyAlias)
        End Function

        Public Shared Function prop(ByVal os As EntityUnion, ByVal propertyAlias As String) As SortLink
            Return New SortLink(os, propertyAlias)
        End Function

        Public Shared Function prop(ByVal oprop As ObjectProperty) As SortLink
            Return New SortLink(oprop.Entity, oprop.PropertyAlias)
        End Function

        Public Shared Function External(ByVal tag As String, ByVal externalSort As ExternalSortDelegate) As SortLink
            Return New SortLink(tag, externalSort)
        End Function

        Public Shared Function External(ByVal tag As String) As SortLink
            Return New SortLink(tag)
        End Function

        'Public Shared Widening Operator CType(ByVal so As SCtor) As Sort
        '    Return so._prev
        'End Operator

        Public Class SortLink
            Private _s As Sort

            Public Sub New(ByVal s As Sort)
                _s = s
            End Sub

            Public Sub New(ByVal eu As EntityUnion, ByVal propertyAlias As String)
                _s = New Sort(eu, propertyAlias, SortType.Asc, False, Nothing)
            End Sub

            Public Sub New(ByVal tbl As SourceFragment, ByVal column As String)
                _s = New Sort(tbl, column, SortType.Asc, False)
            End Sub

            Public Sub New(ByVal format As String, ByVal params() As IFilterValue)
                _s = New Sort(format, params)
            End Sub

            Public Sub New(ByVal q As QueryCmd)
                _s = New Sort(q)
            End Sub

            Public Sub New(ByVal tag As String)
                _s = New Sort(CType(Nothing, EntityUnion), Nothing, SortType.Asc, True, Nothing)
                _s.Column = tag
            End Sub

            Public Sub New(ByVal tag As String, ByVal del As ExternalSortDelegate)
                _s = New Sort(CType(Nothing, EntityUnion), Nothing, SortType.Asc, True, del)
                _s.Column = tag
            End Sub

            Public Sub New(ByVal se As SelectExpression)
                _s = New Sort()
                se.CopyTo(_s)
            End Sub

            Protected Friend Sub New(ByVal prev As Sort, ByVal eu As EntityUnion, ByVal propertyAlias As String)
                _s = New Sort(prev, eu, propertyAlias, SortType.Asc, False, Nothing)
            End Sub

            Protected Friend Sub New(ByVal prev As Sort, ByVal tbl As SourceFragment, ByVal column As String)
                _s = New Sort(prev, tbl, column, SortType.Asc, False, Nothing)
            End Sub

            Protected Friend Sub New(ByVal prev As Sort, ByVal format As String, ByVal params() As IFilterValue)
                _s = New Sort(prev, format, params)
            End Sub

            Protected Friend Sub New(ByVal prev As Sort, ByVal q As QueryCmd)
                _s = New Sort(prev, q)
            End Sub

            Protected Friend Sub New(ByVal prev As Sort, ByVal tag As String)
                _s = New Sort(prev, CType(Nothing, EntityUnion), Nothing, SortType.Asc, True, Nothing)
                _s.Column = tag
            End Sub

            Protected Friend Sub New(ByVal prev As Sort, ByVal tag As String, ByVal del As ExternalSortDelegate)
                _s = New Sort(prev, CType(Nothing, EntityUnion), Nothing, SortType.Asc, True, del)
                _s.Column = tag
            End Sub

            Protected Friend Sub New(ByVal prev As Sort, ByVal se As SelectExpression)
                _s = New Sort(prev)
                se.CopyTo(_s)
            End Sub

            Public Function prop(ByVal t As Type, ByVal propertyAlias As String) As SortLink
                Return New SortLink(_s, New EntityUnion(t), propertyAlias)
            End Function

            Public Function prop(ByVal eu As EntityUnion, ByVal propertyAlias As String) As SortLink
                Return New SortLink(_s, eu, propertyAlias)
            End Function

            Public Function prop(ByVal en As String, ByVal propertyAlias As String) As SortLink
                Return New SortLink(_s, New EntityUnion(en), propertyAlias)
            End Function

            Public Function prop(ByVal propertyAlias As String) As SortLink
                Return New SortLink(_s, _s.ObjectSource, propertyAlias)
            End Function

            Public Function prop(ByVal op As ObjectProperty) As SortLink
                Return New SortLink(_s, op.Entity, op.PropertyAlias)
            End Function

            Public Function column(ByVal clm As String) As SortLink
                Return New SortLink(_s, _s.Table, clm)
            End Function

            Public Function column(ByVal table As SourceFragment, ByVal clm As String) As SortLink
                Return New SortLink(_s, table, clm)
            End Function

            Public Function query(ByVal queryCmd As QueryCmd) As SortLink
                Return New SortLink(_s, queryCmd)
            End Function

            Public Function custom(ByVal sortexpression As String, ByVal ParamArray values() As IFilterValue) As SortLink
                Return New SortLink(_s, sortexpression, values)
            End Function

            Public Function custom(ByVal sortexpression As String, ByVal values() As SelectExpression) As SortLink
                Return New SortLink(_s, sortexpression, Array.ConvertAll(values, Function(se As SelectExpression) New SelectExpressionValue(se)))
            End Function

            Public Function Exp(ByVal se As SelectExpression) As SortLink
                Return New SortLink(_s, se)
            End Function

            Public ReadOnly Property asc() As SortLink
                Get
                    _s.Order = SortType.Asc
                    Return Me
                End Get
            End Property

            Public ReadOnly Property desc() As SortLink
                Get
                    _s.Order = SortType.Desc
                    Return Me
                End Get
            End Property

            Public Function Order(ByVal o As SortType) As SortLink
                _s.Order = o
                Return Me
            End Function

            Public Function Order(ByVal asc As Boolean) As SortLink
                If asc Then
                    Return Me.asc
                Else
                    Return Me.desc
                End If
            End Function

            Public Function Order(ByVal sortType As String) As SortLink
                _s.Order = CType([Enum].Parse(GetType(SortType), sortType, True), SortType)
                Return Me
            End Function

            Public Shared Widening Operator CType(ByVal so As SortLink) As Sort
                Return so._s
            End Operator
        End Class
    End Class

    '    Public Class SortLink
    '        Private _f As String
    '        'Private _prop As OrmProperty
    '        Private _ext As Boolean
    '        Private _prev As SortLink
    '        Private _order As SortType
    '        Private _os As EntityUnion
    '        'Private _custom As String
    '        'Private _values() As FieldReference
    '        Private _del As ExternalSortDelegate
    '        Private _table As SourceFragment
    '        Private _cmd As QueryCmd
    '        Private _custom As CustomValue

    '        Protected Sub New()

    '        End Sub

    '        Protected Friend Shared Function CreateCustom(ByVal sortExpression As String, _
    '           ByVal prev As SortLink, ByVal values() As IFilterValue) As SortLink
    '            Dim s As New SortLink(prev, sortExpression, values)
    '            's._custom = sortExpression
    '            's._values = values
    '            Return s
    '        End Function

    '#Region " Type ctors "

    '        Protected Friend Sub New(ByVal t As Type, ByVal prev As SortLink)
    '            _prev = prev
    '            _os = New EntityUnion(t)
    '        End Sub

    '        Protected Friend Sub New(ByVal t As Type, ByVal propertyAlias As String, Optional ByVal prev As SortLink = Nothing)
    '            _f = propertyAlias
    '            _prev = prev
    '            _os = New EntityUnion(t)
    '        End Sub

    '        Protected Friend Sub New(ByVal t As Type, ByVal propertyAlias As String, ByVal ext As Boolean, Optional ByVal prev As SortLink = Nothing)
    '            _f = propertyAlias
    '            _ext = ext
    '            _prev = prev
    '            If t IsNot Nothing Then
    '                _os = New EntityUnion(t)
    '            End If
    '        End Sub

    '        Protected Friend Sub New(ByVal t As Type, ByVal propertyAlias As String, ByVal ext As Boolean, ByVal del As ExternalSortDelegate)
    '            _f = propertyAlias
    '            _ext = ext
    '            If t IsNot Nothing Then
    '                _os = New EntityUnion(t)
    '            End If
    '            _del = del
    '        End Sub

    '        Protected Friend Sub New(ByVal os As EntityUnion, ByVal propertyAlias As String)
    '            _os = os
    '            _f = propertyAlias
    '        End Sub

    '        Protected Friend Sub New(ByVal os As EntityUnion, ByVal propertyAlias As String, ByVal prev As SortLink)
    '            _prev = prev
    '            _os = os
    '            _f = propertyAlias
    '        End Sub

    '        Protected Friend Sub New(ByVal os As EntityUnion, ByVal propertyAlias As String, ByVal ext As Boolean, Optional ByVal prev As SortLink = Nothing)
    '            _f = propertyAlias
    '            _ext = ext
    '            _prev = prev
    '            _os = os
    '        End Sub
    '#End Region

    '#Region " Table ctor "

    '        Protected Friend Sub New(ByVal t As SourceFragment, ByVal prev As SortLink)
    '            _prev = prev
    '            _table = t
    '        End Sub

    '        Protected Friend Sub New(ByVal t As SourceFragment, ByVal f As String, Optional ByVal prev As SortLink = Nothing)
    '            _f = f
    '            _prev = prev
    '            _table = t
    '        End Sub

    '        Protected Friend Sub New(ByVal t As SourceFragment, ByVal f As String, ByVal ext As Boolean, Optional ByVal prev As SortLink = Nothing)
    '            _f = f
    '            _ext = ext
    '            _prev = prev
    '            _table = t
    '        End Sub

    '        Protected Friend Sub New(ByVal t As SourceFragment, ByVal f As String, ByVal ext As Boolean, ByVal del As ExternalSortDelegate)
    '            _f = f
    '            _ext = ext
    '            _table = t
    '            _del = del
    '        End Sub

    '#End Region

    '        Protected Friend Sub New(ByVal prev As SortLink, ByVal sortExpression As String, _
    '                                 ByVal values() As IFilterValue)
    '            _prev = prev
    '            _custom = New CustomValue(sortExpression, values)
    '        End Sub

    '        Protected Friend Sub New(ByVal cmd As QueryCmd)
    '            _cmd = cmd
    '        End Sub

    '        Protected Friend Sub New(ByVal cmd As QueryCmd, ByVal prev As SortLink)
    '            _cmd = cmd
    '            _prev = prev
    '        End Sub

    '        Protected Friend Sub New(ByVal se As SelectExpression)
    '            _cmd = se.Query
    '            _custom = se.custom
    '            _f = se.Column
    '            _os = se.ObjectSource
    '            _table = se.Table
    '        End Sub

    '        Protected Friend Sub New(ByVal se As SelectExpression, ByVal prev As SortLink)
    '            _prev = prev
    '            _cmd = se.Query
    '            _custom = se.Custom
    '            _f = se.Column
    '            _os = se.ObjectSource
    '            _table = se.Table
    '            '_values = se.Values
    '        End Sub

    '        Public Function Sort(ByVal so As SortLink) As SortLink
    '            _prev = so
    '            Return Me
    '        End Function

    '        Public Function query(ByVal queryCmd As QueryCmd) As SortLink
    '            Return New SortLink(queryCmd, Me)
    '        End Function

    '        Public Function prop(ByVal entityName As String, ByVal propertyAlias As String) As SortLink
    '            Return New SortLink(New EntityUnion(entityName), propertyAlias, Me)
    '        End Function

    '        Public Function prop(ByVal t As Type, ByVal propertyAlias As String) As SortLink
    '            Return New SortLink(t, propertyAlias, Me)
    '        End Function

    '        Public Function prop(ByVal propertyAlias As String) As SortLink
    '            Return New SortLink(_os, propertyAlias, Me)
    '        End Function

    '        Public Function prop(ByVal oprop As ObjectProperty) As SortLink
    '            Return New SortLink(oprop.ObjectSource, oprop.Field, Me)
    '        End Function

    '        Public Function column(ByVal clm As String) As SortLink
    '            Return New SortLink(_table, clm, Me)
    '        End Function

    '        Public Function column(ByVal table As SourceFragment, ByVal clm As String) As SortLink
    '            Return New SortLink(table, clm, Me)
    '        End Function

    '        Public Function Exp(ByVal se As SelectExpression) As SortLink
    '            Return New SortLink(se, Me)
    '        End Function

    '        Public Function External(ByVal tag As String) As SortLink
    '            If _os IsNot Nothing Then
    '                Return New SortLink(_os, tag, True, Me)
    '            Else
    '                Return New SortLink(_table, tag, True, Me)
    '            End If
    '        End Function

    '        Public Function prop(ByVal t As SourceFragment, ByVal propertyAlias As String) As SortLink
    '            Return New SortLink(t, propertyAlias, Me)
    '        End Function

    '        Public Function custom(ByVal sortexpression As String, ByVal ParamArray values() As IFilterValue) As SortLink
    '            Return CreateCustom(sortexpression, Me, values)
    '        End Function

    '        Public Function custom(ByVal sortexpression As String, ByVal values() As SelectExpression) As SortLink
    '            Return CreateCustom(sortexpression, Me, Array.ConvertAll(values, Function(se As SelectExpression) New SelectExpressionValue(se)))
    '        End Function

    '        Public ReadOnly Property asc() As SortLink
    '            Get
    '                If IsCustom Then
    '                    Throw New InvalidOperationException("Sort is custom")
    '                End If
    '                _order = SortType.Asc
    '                Return Me
    '                'Return New Sort(_f, SortType.Asc, _ext)
    '            End Get
    '        End Property

    '        Public ReadOnly Property desc() As SortLink
    '            Get
    '                'If IsCustom Then
    '                '    Throw New InvalidOperationException("Sort is custom")
    '                'End If
    '                _order = SortType.Desc
    '                Return Me
    '                'Return New Sort(_f, SortType.Desc, _ext)
    '            End Get
    '        End Property

    '        Public Function Order(ByVal o As SortType) As SCtor
    '            _order = o
    '            Return New SCtor(_os, Me)
    '        End Function

    '        Public Function Order(ByVal asc As Boolean) As SortLink
    '            If IsCustom Then
    '                Throw New InvalidOperationException("Sort is custom")
    '            End If

    '            If asc Then
    '                Return Me.asc 'New Sort(_f, SortType.Asc, _ext)
    '            Else
    '                Return Me.desc 'New Sort(_f, SortType.Desc, _ext)
    '            End If
    '        End Function

    '        Public Function Order(ByVal orderParam As String) As SortLink
    '            If IsCustom Then
    '                Throw New InvalidOperationException("Sort is custom")
    '            End If

    '            _order = CType([Enum].Parse(GetType(SortType), orderParam, True), SortType)
    '            Return Me 'New Sort(_f, _, _ext)
    '        End Function

    '        Public Shared Widening Operator CType(ByVal so As SortLink) As Sort
    '            Return xxx(so)
    '        End Operator

    '        Private Shared Function xxx(ByVal so As SortLink) As Sort
    '            If Not String.IsNullOrEmpty(so._f) OrElse so._custom IsNot Nothing OrElse so._cmd IsNot Nothing Then
    '                If so._prev Is Nothing Then
    '                    If so.IsCustom Then
    '                        Dim s As New Sort(so._custom)
    '                        s._order = so._order
    '                        Return s
    '                    Else
    '                        If so._os IsNot Nothing Then
    '                            Return New Sort(so._os, so._f, so._order, so._ext, so._del)
    '                        ElseIf so._cmd IsNot Nothing Then
    '                            Return New Sort(so._cmd, so._order)
    '                        Else
    '                            Return New Sort(so._table, so._f, so._order, so._ext, so._del)
    '                        End If
    '                    End If
    '                Else
    '                    If so.IsCustom Then
    '                        Dim s As New Sort(so._prev, so._custom)
    '                        s._order = so._order
    '                        Return s
    '                    Else
    '                        If so._os IsNot Nothing Then
    '                            Return New Sort(so._prev, so._os, so._f, so._order, so._ext, so._del)
    '                        ElseIf so._cmd IsNot Nothing Then
    '                            Return New Sort(so._prev, so._cmd, so._order)
    '                        Else
    '                            Return New Sort(so._prev, so._table, so._f, so._order, so._ext, so._del)
    '                        End If
    '                    End If
    '                End If
    '            Else
    '                Return so._prev
    '            End If
    '        End Function

    '        Public ReadOnly Property IsCustom() As Boolean
    '            Get
    '                Return _custom IsNot Nothing
    '            End Get
    '        End Property

    '    End Class

End Namespace