Imports Worm.Orm
Imports Worm.Sorting
Imports Worm.Orm.Meta

Namespace Orm
    Public Enum SortType
        Asc
        Desc
    End Enum

    Public Class Sorting
        Private _t As Type
        Private _prev As SortOrder

        Protected Friend Sub New(ByVal t As Type, ByVal prev As SortOrder)
            _t = t
            _prev = prev
        End Sub

        Public Function NextField(ByVal fieldName As String) As SortOrder
            Return New SortOrder(_t, fieldName, _prev)
        End Function

        Public Function NextExternal(ByVal fieldName As String) As SortOrder
            Return New SortOrder(_t, fieldName, True, _prev)
        End Function

        Public Shared Function Field(ByVal fieldName As String) As SortOrder
            Return New SortOrder(Nothing, fieldName)
        End Function

        Public Shared Function Custom(ByVal sortExpression As String) As SortOrder
            Return SortOrder.CreateCustom(Nothing, sortExpression, Nothing)
        End Function

        Public Shared Function Custom(ByVal t As Type, ByVal sortExpression As String) As SortOrder
            Return SortOrder.CreateCustom(t, sortExpression, Nothing)
        End Function

        Public Shared Function External(ByVal fieldName As String) As SortOrder
            Return New SortOrder(Nothing, fieldName, True)
        End Function

        Public Shared Function Field(ByVal t As Type, ByVal fieldName As String) As SortOrder
            Return New SortOrder(t, fieldName)
        End Function

        Public Shared Function External(ByVal t As Type, ByVal fieldName As String) As SortOrder
            Return New SortOrder(t, fieldName, True)
        End Function

        Public Shared Widening Operator CType(ByVal so As Sorting) As Sort
            Return so._prev
        End Operator

        Public Shared Function Any() As Sort
            Return New Sort
        End Function
    End Class

End Namespace

Namespace Sorting

    Public Class SortOrder
        Private _f As String
        Private _ext As Boolean
        Private _prev As SortOrder
        Private _order As SortType
        Private _t As Type
        Private _custom As String

        Protected Friend Sub New(ByVal t As Type, ByVal prev As SortOrder)
            _prev = prev
            _t = t
        End Sub

        Protected Friend Shared Function CreateCustom(ByVal t As Type, ByVal sortExpression As String, ByVal prev As SortOrder) As SortOrder
            Dim s As New SortOrder(t, prev)
            s._custom = sortExpression
            Return s
        End Function

        Protected Friend Sub New(ByVal t As Type, ByVal f As String, Optional ByVal prev As SortOrder = Nothing)
            _f = f
            _prev = prev
            _t = t
        End Sub

        Protected Friend Sub New(ByVal t As Type, ByVal f As String, ByVal ext As Boolean, Optional ByVal prev As SortOrder = Nothing)
            _f = f
            _ext = ext
            _prev = prev
            _t = t
        End Sub

        'Public Function NextSort(ByVal field As String) As SortOrder
        '    _f = field
        '    Return Me
        'End Function

        'Public Function NextSort(ByVal t As Type, ByVal field As String) As SortOrder
        '    _f = field
        '    _t = t
        '    Return Me
        'End Function

        Public Function NextField(ByVal t As Type, ByVal fieldName As String) As SortOrder
            Return New SortOrder(t, fieldName, Me)
        End Function

        Public Function NextField(ByVal fieldName As String) As SortOrder
            Return New SortOrder(_t, fieldName, Me)
        End Function

        Public Function NextExternal(ByVal fieldName As String) As SortOrder
            Return New SortOrder(_t, fieldName, True, Me)
        End Function

        Public Function NextCustom(ByVal sortexpression As String) As SortOrder
            Return CreateCustom(_t, sortexpression, Me)
        End Function

        Public ReadOnly Property Asc() As Orm.Sorting
            Get
                If IsCustom Then
                    Throw New InvalidOperationException("Sort is custom")
                End If
                _order = SortType.Asc
                Return New Orm.Sorting(_t, Me)
                'Return New Sort(_f, SortType.Asc, _ext)
            End Get
        End Property

        Public ReadOnly Property Desc() As Orm.Sorting
            Get
                If IsCustom Then
                    Throw New InvalidOperationException("Sort is custom")
                End If
                _order = SortType.Desc
                Return New Orm.Sorting(_t, Me)
                'Return New Sort(_f, SortType.Desc, _ext)
            End Get
        End Property

        Public Function Order(ByVal orderParam As Boolean) As Orm.Sorting
            If IsCustom Then
                Throw New InvalidOperationException("Sort is custom")
            End If

            If orderParam Then
                Return Asc 'New Sort(_f, SortType.Asc, _ext)
            Else
                Return Desc 'New Sort(_f, SortType.Desc, _ext)
            End If
        End Function

        Public Function Order(ByVal orderParam As String) As Orm.Sorting
            If IsCustom Then
                Throw New InvalidOperationException("Sort is custom")
            End If

            _order = CType([Enum].Parse(GetType(SortType), orderParam, True), SortType)
            Return New Orm.Sorting(_t, Me) 'New Sort(_f, _, _ext)
        End Function

        Public Shared Widening Operator CType(ByVal so As SortOrder) As Sort
            If Not String.IsNullOrEmpty(so._f) OrElse Not String.IsNullOrEmpty(so._custom) Then
                If so._prev Is Nothing Then
                    If so.IsCustom Then
                        Return New Sort(so._t, so._custom)
                    Else
                        Return New Sort(so._t, so._f, so._order, so._ext)
                    End If
                Else
                    If so.IsCustom Then
                        Return New Sort(so._prev, so._t, so._custom)
                    Else
                        Return New Sort(so._prev, so._t, so._f, so._order, so._ext)
                    End If
                End If
            Else
                Return so._prev
            End If
        End Operator

        Public ReadOnly Property IsCustom() As Boolean
            Get
                Return Not String.IsNullOrEmpty(_custom)
            End Get
        End Property

    End Class

    Public Class Sort
        Private _f As String
        Private _order As SortType
        Private _any As Boolean
        Private _custom As String

        Private _ext As Boolean

        Private _prev As Sort
        Private _t As Type

        Protected Friend Sub New(ByVal prev As Sort, ByVal t As Type, ByVal fieldName As String, ByVal order As SortType, ByVal external As Boolean)
            _f = fieldName
            _order = order
            _ext = external
            _t = t
            _prev = prev
        End Sub

        Public Sub New(ByVal t As Type, ByVal sortExpression As String)
            _t = t
            _custom = sortExpression
        End Sub

        Public Sub New(ByVal prev As Sort, ByVal t As Type, ByVal sortExpression As String)
            _prev = prev
            _t = t
            _custom = sortExpression
        End Sub

        Public Sub New(ByVal t As Type, ByVal fieldName As String, ByVal order As SortType, ByVal external As Boolean)
            _f = fieldName
            _order = order
            _ext = external
            _t = t
        End Sub

        Public Sub New(ByVal fieldName As String, ByVal order As SortType, ByVal external As Boolean)
            _f = fieldName
            _order = order
            _ext = external
        End Sub

        Protected Friend Sub New()
            _any = True
        End Sub

        Public Property CustomSortExpression() As String
            Get
                Return _custom
            End Get
            Set(ByVal value As String)
                _custom = value
            End Set
        End Property

        Public Property Type() As Type
            Get
                Return _t
            End Get
            Set(ByVal value As Type)
                _t = value
            End Set
        End Property

        Public Property FieldName() As String
            Get
                Return _f
            End Get
            Set(ByVal value As String)
                _f = value
            End Set
        End Property

        Public Property Order() As SortType
            Get
                Return _order
            End Get
            Set(ByVal value As SortType)
                _order = value
            End Set
        End Property

        Public Property IsExternal() As Boolean
            Get
                Return _ext
            End Get
            Set(ByVal value As Boolean)
                _ext = value
            End Set
        End Property

        Public ReadOnly Property IsCustom() As Boolean
            Get
                Return Not String.IsNullOrEmpty(_custom)
            End Get
        End Property

        Public ReadOnly Property IsAny() As Boolean
            Get
                Return _any
            End Get
        End Property

        Public Overrides Function ToString() As String
            Dim s As String = Nothing
            If Not String.IsNullOrEmpty(_custom) Then
                s = _custom
            Else
                s = _f & _order.ToString & _ext.ToString
            End If

            If _t IsNot Nothing Then
                s &= _t.ToString
            End If

            Return s
        End Function

        Public Overrides Function Equals(ByVal obj As Object) As Boolean
            Return Equals(TryCast(obj, Sort))
        End Function

        Public Overloads Function Equals(ByVal s As Sort) As Boolean
            If s Is Nothing Then
                Return False
            Else
                Dim b As Boolean
                If Not String.IsNullOrEmpty(_custom) Then
                    b = _custom = s._custom AndAlso _t Is s._t
                Else
                    b = _f = s._f AndAlso _order = s._order AndAlso _ext = s._ext AndAlso _t Is s._t
                End If

                If b Then
                    If Not _prev Is s._prev Then
                        If _prev IsNot Nothing Then
                            b = _prev.Equals(s._prev)
                        Else
                            b = False
                        End If
                    End If
                End If
                Return b
            End If
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return ToString.GetHashCode
        End Function

        Protected Friend ReadOnly Property Previous() As Sort
            Get
                Return _prev
            End Get
        End Property
    End Class

    Public Class OrmComparer(Of T As {OrmBase})
        Implements Generic.IComparer(Of T), IComparer

        Public Delegate Function GetObjectDelegate(ByVal x As T, ByVal t As Type) As OrmBase

        'Private _q As Generic.List(Of Sort)
        Private _mgr As OrmManagerBase
        Private _s As Generic.Stack(Of Sort)
        Private _t As Type
        Private _getobj As GetObjectDelegate

        Public Sub New(ByVal t As Type, ByVal q As Generic.Stack(Of Sort))
            _s = q
            _mgr = OrmManagerBase.CurrentManager
            _t = t
        End Sub

        Public Sub New(ByVal t As Type, ByVal s As Sort)
            _t = t
            _s = New Generic.Stack(Of Sort)
            _s.Push(s)
            _mgr = OrmManagerBase.CurrentManager
        End Sub

        Public Sub New(ByVal s As Sort)
            _t = GetType(T)
            _s = New Generic.Stack(Of Sort)
            _s.Push(s)
            _mgr = OrmManagerBase.CurrentManager
        End Sub

        Public Sub New(ByVal q As Generic.Stack(Of Sort))
            _s = q
            _mgr = OrmManagerBase.CurrentManager
            _t = GetType(T)
        End Sub

        Public Sub New(ByVal q As Generic.Stack(Of Sort), ByVal getObj As GetObjectDelegate)
            MyClass.New(q)
            _getobj = getObj
        End Sub

        Public Sub New(ByVal fieldName As String, ByVal st As SortType)
            '_q = New Generic.List(Of Sort)
            _s = New Generic.Stack(Of Sort)
            _s.Push(New Sort(GetType(T), fieldName, st, False))
            _mgr = OrmManagerBase.CurrentManager
            _t = GetType(T)
        End Sub

        Public Function Compare(ByVal x As T, ByVal y As T) As Integer Implements System.Collections.Generic.IComparer(Of T).Compare
            Dim p As Integer = 0
            For Each s As Sort In _s
                If s.IsAny Then
                    Throw New NotSupportedException("Any sorting is not supported")
                End If
                Dim ss As IOrmObjectSchemaBase = Nothing
                Dim xo As Object = GetValue(x, s, ss)
                Dim yo As Object = GetValue(y, s, ss)
                Dim pr2 As Pair(Of OrmBase, IOrmSorting) = TryCast(yo, Pair(Of OrmBase, IOrmSorting))
                If pr2 IsNot Nothing Then
                    Dim c As IComparer = pr2.Second.CreateSortComparer(s)
                    If c IsNot Nothing Then
                        p = c.Compare(CType(xo, Pair(Of OrmBase, IOrmSorting)).First, pr2.First)
                        If p = 0 Then
                            Continue For
                        Else
                            Exit For
                        End If
                    Else
                        Dim pr As Pair(Of OrmBase, IOrmSorting) = TryCast(xo, Pair(Of OrmBase, IOrmSorting))
                        xo = pr.First.GetValue(s.FieldName)
                        yo = pr2.First.GetValue(s.FieldName)
                    End If
                End If
                Dim k As Integer = 1
                If s.Order = SortType.Desc Then
                    k = -1
                End If
                If xo Is Nothing Then
                    If yo Is Nothing Then
                        Continue For
                    Else
                        p = -1 * k
                        Exit For
                    End If
                Else
                    If yo Is Nothing Then
                        p = 1 * k
                        Exit For
                    Else
                        Dim xc As IComparable = TryCast(xo, IComparable)
                        Dim yc As IComparable = TryCast(yo, IComparable)
                        If xc Is Nothing OrElse yc Is Nothing Then
                            Throw New InvalidOperationException("Value " & s.FieldName & " of type " & s.Type.ToString & " is not supported IComparable")
                        End If
                        p = xc.CompareTo(yc) * k
                        If p <> 0 Then
                            Exit For
                        End If
                    End If
                End If
            Next
            Return p
        End Function

        Private Function GetValue(ByVal x As T, ByVal s As Sort, ByRef oschema As IOrmObjectSchemaBase) As Object
            Dim xo As OrmBase = x
            If s.Type IsNot Nothing AndAlso _t IsNot s.Type Then
                Dim schema As OrmSchemaBase = _mgr.ObjectSchema
                If _getobj IsNot Nothing Then
                    xo = _getobj(x, s.Type)
                Else
                    xo = schema.GetJoinObj(schema.GetObjectSchema(_t), xo, s.Type)
                End If
                If oschema Is Nothing Then
                    oschema = schema.GetObjectSchema(s.Type)
                End If
                Dim ss As IOrmSorting = TryCast(oschema, IOrmSorting)
                If ss IsNot Nothing Then
                    Return New Pair(Of OrmBase, IOrmSorting)(xo, ss)
                End If
            End If
            Return xo.GetValue(s.FieldName)
        End Function

        Private Function _Compare(ByVal x As Object, ByVal y As Object) As Integer Implements System.Collections.IComparer.Compare
            Return Compare(TryCast(x, T), TryCast(y, T))
        End Function
    End Class
End Namespace