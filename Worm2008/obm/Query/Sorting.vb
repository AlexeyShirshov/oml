Imports Worm.Orm
Imports Worm.Sorting
Imports Worm.Orm.Meta
Imports System.Collections.Generic

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
            Return New SortOrder(CType(Nothing, Type), fieldName)
        End Function

        'Public Shared Function Column(ByVal clm As String) As SortOrder
        '    Return New SortOrder(CType(Nothing, SourceFragment), clm)
        'End Function

        'Public Shared Function Custom(ByVal sortExpression As String, ByVal values() As Pair(Of Object, String)) As SortOrder
        '    Return SortOrder.CreateCustom(sortExpression, Nothing, values)
        'End Function

        Public Shared Function Custom(ByVal sortExpression As String) As SortOrder
            Return SortOrder.CreateCustom(sortExpression, Nothing, Nothing)
        End Function

        Public Shared Function Custom(ByVal sortExpression As String, ByVal values() As Pair(Of Object, String)) As SortOrder
            Return SortOrder.CreateCustom(sortExpression, Nothing, values)
        End Function

        Public Shared Function External(ByVal tag As String) As SortOrder
            Return New SortOrder(CType(Nothing, Type), tag, True)
        End Function

        Public Shared Function Field(ByVal t As Type, ByVal fieldName As String) As SortOrder
            Return New SortOrder(t, fieldName)
        End Function

        Public Shared Function External(ByVal tag As String, ByVal externalSort As ExternalSortDelegate) As SortOrder
            Return New SortOrder(CType(Nothing, Type), tag, True, externalSort)
        End Function

        Public Shared Widening Operator CType(ByVal so As Sorting) As Sort
            Return so._prev
        End Operator

        'Public Shared Function Any() As Sort
        '    Return New Sort
        'End Function
    End Class

End Namespace

Namespace Sorting

    Public Delegate Function ExternalSortDelegate(ByVal mgr As OrmManager, ByVal generator As ObjectMappingEngine, ByVal sort As Sort, ByVal objs As ICollection) As ICollection

    Public Class SortOrder
        Private _f As String
        'Private _prop As OrmProperty
        Private _ext As Boolean
        Private _prev As SortOrder
        Private _order As SortType
        Private _t As Type
        Private _custom As String
        Private _values() As Pair(Of Object, String)
        Private _del As ExternalSortDelegate
        Private _table As SourceFragment

        Protected Sub New()

        End Sub

        Protected Friend Shared Function CreateCustom(ByVal sortExpression As String, _
           ByVal prev As SortOrder, ByVal values() As Pair(Of Object, String)) As SortOrder
            Dim s As New SortOrder(prev, sortExpression, values)
            's._custom = sortExpression
            's._values = values
            Return s
        End Function

#Region " Type ctors "

        Protected Friend Sub New(ByVal t As Type, ByVal prev As SortOrder)
            _prev = prev
            _t = t
        End Sub

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

        Protected Friend Sub New(ByVal t As Type, ByVal f As String, ByVal ext As Boolean, ByVal del As ExternalSortDelegate)
            _f = f
            _ext = ext
            _t = t
            _del = del
        End Sub

#End Region

#Region " Table ctor "

        Protected Friend Sub New(ByVal t As SourceFragment, ByVal prev As SortOrder)
            _prev = prev
            _table = t
        End Sub

        Protected Friend Sub New(ByVal t As SourceFragment, ByVal f As String, Optional ByVal prev As SortOrder = Nothing)
            _f = f
            _prev = prev
            _table = t
        End Sub

        Protected Friend Sub New(ByVal t As SourceFragment, ByVal f As String, ByVal ext As Boolean, Optional ByVal prev As SortOrder = Nothing)
            _f = f
            _ext = ext
            _prev = prev
            _table = t
        End Sub

        Protected Friend Sub New(ByVal t As SourceFragment, ByVal f As String, ByVal ext As Boolean, ByVal del As ExternalSortDelegate)
            _f = f
            _ext = ext
            _table = t
            _del = del
        End Sub

#End Region

        Protected Friend Sub New(ByVal prev As SortOrder, ByVal sortExpression As String, ByVal values() As Pair(Of Object, String))
            _prev = prev
            _custom = sortExpression
            _values = values
        End Sub

        'Public Function NextSort(ByVal field As String) As SortOrder
        '    _f = field
        '    Return Me
        'End Function

        Public Function NextSort(ByVal so As SortOrder) As SortOrder
            _prev = so
            Return Me
        End Function

        Public Function NextField(ByVal t As Type, ByVal fieldName As String) As SortOrder
            Return New SortOrder(t, fieldName, Me)
        End Function

        Public Function NextField(ByVal fieldName As String) As SortOrder
            If _t IsNot Nothing Then
                Return New SortOrder(_t, fieldName, Me)
            Else
                Return New SortOrder(_table, fieldName, Me)
            End If
        End Function

        Public Function NextExternal(ByVal fieldName As String) As SortOrder
            If _t IsNot Nothing Then
                Return New SortOrder(_t, fieldName, True, Me)
            Else
                Return New SortOrder(_table, fieldName, True, Me)
            End If
        End Function

        Public Function NextField(ByVal t As SourceFragment, ByVal fieldName As String) As SortOrder
            Return New SortOrder(t, fieldName, Me)
        End Function

        Public Function NextCustom(ByVal sortexpression As String, ByVal values() As Pair(Of Object, String)) As SortOrder
            Return CreateCustom(sortexpression, Me, values)
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
                        Return New Sort(so._custom, so._values)
                    Else
                        If so._t IsNot Nothing Then
                            Return New Sort(so._t, so._f, so._order, so._ext, so._del)
                        Else
                            Return New Sort(so._table, so._f, so._order, so._ext, so._del)
                        End If
                    End If
                Else
                    If so.IsCustom Then
                        Return New Sort(so._prev, so._custom, so._values)
                    Else
                        If so._t IsNot Nothing Then
                            Return New Sort(so._prev, so._t, so._f, so._order, so._ext, so._del)
                        Else
                            Return New Sort(so._prev, so._table, so._f, so._order, so._ext, so._del)
                        End If
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
        Inherits SelectExpression
        Implements ICloneable

        'Private _f As String
        Private _order As SortType
        'Private _any As Boolean
        'Private _custom As String
        'Private _values() As Pair(Of Object, String)
        Private _ext As Boolean
        Private _del As ExternalSortDelegate

        Private _prev As Sort
        'Private _t As Type
        'Private _table As SourceFragment

        'Public Event OnChange()

        Public Class Iterator
            Implements IEnumerable(Of Sort), IEnumerator(Of Sort)

            Private _s As Sort
            Private _c As Sort

            Public Sub New(ByVal s As Sort)
                _s = s
            End Sub

            Public Function GetEnumerator() As System.Collections.Generic.IEnumerator(Of Sort) Implements System.Collections.Generic.IEnumerable(Of Sort).GetEnumerator
                Return Me
            End Function

            Public ReadOnly Property Current() As Sort Implements System.Collections.Generic.IEnumerator(Of Sort).Current
                Get
                    Return _c
                End Get
            End Property

            Private Function _GetEnumerator() As System.Collections.IEnumerator Implements System.Collections.IEnumerable.GetEnumerator
                Return GetEnumerator()
            End Function

            Private ReadOnly Property _Current() As Object Implements System.Collections.IEnumerator.Current
                Get
                    Return Current
                End Get
            End Property

            Public Function MoveNext() As Boolean Implements System.Collections.IEnumerator.MoveNext
                If _c Is Nothing Then
                    _c = _s
                Else
                    _c = _c.Previous
                End If
                Return _c IsNot Nothing
            End Function

            Public Sub Reset() Implements System.Collections.IEnumerator.Reset
                _c = Nothing
            End Sub

#Region " IDisposable Support "
            Private disposedValue As Boolean = False        ' To detect redundant calls

            ' IDisposable
            Protected Overridable Sub Dispose(ByVal disposing As Boolean)
                If Not Me.disposedValue Then
                    If disposing Then
                        ' TODO: free other state (managed objects).
                    End If

                    ' TODO: free your own state (unmanaged objects).
                    ' TODO: set large fields to null.
                End If
                Me.disposedValue = True
            End Sub

            ' This code added by Visual Basic to correctly implement the disposable pattern.
            Public Sub Dispose() Implements IDisposable.Dispose
                ' Do not change this code.  Put cleanup code in Dispose(ByVal disposing As Boolean) above.
                Dispose(True)
                GC.SuppressFinalize(Me)
            End Sub
#End Region

        End Class

#Region " Table ctors "

        Protected Friend Sub New(ByVal prev As Sort, ByVal t As SourceFragment, ByVal column As String, ByVal order As SortType, ByVal external As Boolean, ByVal del As ExternalSortDelegate)
            MyBase.New(t, column)
            '_f = fieldName
            _order = order
            _ext = external
            '_table = t
            _prev = prev
            _del = del
        End Sub

        Public Sub New(ByVal t As SourceFragment, ByVal column As String, ByVal order As SortType, ByVal external As Boolean)
            MyBase.New(t, column)
            '_f = fieldName
            _order = order
            _ext = external
            '_table = t
        End Sub

        Public Sub New(ByVal t As SourceFragment, ByVal column As String, ByVal order As SortType, ByVal external As Boolean, ByVal del As ExternalSortDelegate)
            MyBase.New(t, column)
            '_f = fieldName
            _order = order
            _ext = external
            '_table = t
            _del = del
        End Sub

#End Region

#Region " Type ctors "

        Protected Friend Sub New(ByVal prev As Sort, ByVal t As Type, ByVal fieldName As String, ByVal order As SortType, ByVal external As Boolean, ByVal del As ExternalSortDelegate)
            MyBase.New(t, fieldName)
            '_f = fieldName
            _order = order
            _ext = external
            '_t = t
            _prev = prev
            _del = del
        End Sub

        Public Sub New(ByVal t As Type, ByVal fieldName As String, ByVal order As SortType, ByVal external As Boolean)
            MyBase.New(t, fieldName)
            '_f = fieldName
            _order = order
            _ext = external
            '_t = t
        End Sub

        Public Sub New(ByVal t As Type, ByVal fieldName As String, ByVal order As SortType, ByVal external As Boolean, ByVal del As ExternalSortDelegate)
            MyBase.New(t, fieldName)
            '_f = fieldName
            _order = order
            _ext = external
            '_t = t
            _del = del
        End Sub

#End Region

#Region " Typeless ctors "

        Public Sub New(ByVal sortExpression As String, ByVal values() As Pair(Of Object, String))
            '    '_t = t
            MyBase.New(sortExpression, values)
            '    _custom = sortExpression
            '    _values = values
        End Sub

        Public Sub New(ByVal prev As Sort, ByVal sortExpression As String, ByVal values() As Pair(Of Object, String))
            MyBase.New(sortExpression, values)
            _prev = prev
            '_t = t
            '_custom = sortExpression
            '_values = values
        End Sub

        Public Sub New(ByVal fieldName As String, ByVal order As SortType, ByVal external As Boolean)
            MyBase.New(fieldName)
            '_f = fieldName
            _order = order
            _ext = external
        End Sub

        Public Sub New(ByVal fieldName As String, ByVal order As SortType, ByVal external As Boolean, ByVal del As ExternalSortDelegate)
            MyBase.New(fieldName)
            '_f = fieldName
            _order = order
            _ext = external
            _del = del
        End Sub

#End Region

        Public Sub New(ByVal agr As Query.AggregateBase, ByVal order As SortType)
            MyBase.New(agr)
            _order = order
        End Sub

        Public Sub New(ByVal q As Query.QueryCmd, ByVal order As SortType)
            MyBase.New(q)
            _order = order
        End Sub

        Public Sub New(ByVal q As Query.QueryCmd)
            MyBase.New(q)
        End Sub

        Protected Sub New()
        End Sub

        'Protected Sub RaiseOnChange()
        '    RaiseEvent OnChange()
        'End Sub

        'Public ReadOnly Property Values() As Pair(Of Object, String)()
        '    Get
        '        Return _values
        '    End Get
        'End Property

        Public Property CustomSortExpression() As String
            Get
                Return Computed
            End Get
            Set(ByVal value As String)
                Computed = value
            End Set
        End Property

        'Public Function GetCustomExpressionValues(ByVal schema As ObjectMappingEngine, ByVal aliases As IDictionary(Of SourceFragment, String)) As String()
        '    Return ObjectMappingEngine.ExtractValues(schema, aliases, _values).ToArray
        'End Function

        'Public ReadOnly Property Table() As SourceFragment
        '    Get
        '        Return _table
        '    End Get
        'End Property

        'Public Property Type() As Type
        '    Get
        '        Return _t
        '    End Get
        '    Set(ByVal value As Type)
        '        _t = value
        '        RaiseOnChange()
        '    End Set
        'End Property

        Public Property FieldName() As String
            Get
                If Not String.IsNullOrEmpty(Field) Then
                    Return Field
                Else
                    Return Column
                End If
            End Get
            Set(ByVal value As String)
                Field = value
            End Set
        End Property

        Public Property Order() As SortType
            Get
                Return _order
            End Get
            Protected Set(ByVal value As SortType)
                _order = value
                RaiseOnChange()
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

        'Public ReadOnly Property IsCustom() As Boolean
        '    Get
        '        Return Not String.IsNullOrEmpty(_custom)
        '    End Get
        'End Property

        'Public ReadOnly Property IsAny() As Boolean
        '    Get
        '        Return _any
        '    End Get
        'End Property

        Public Overrides Function ToString() As String
            Throw New NotSupportedException
        End Function

        Public Overrides Function _ToString() As String
            'Dim s As String = Nothing
            'If Not String.IsNullOrEmpty(_custom) Then
            '    s = _custom
            'Else
            '    s = _f & _order.ToString & _ext.ToString
            'End If

            'If _t IsNot Nothing Then
            '    s &= _t.ToString
            'End If

            'Return s
            Return MyBase._ToString & _order.ToString & _ext.ToString
        End Function

        'Public Overrides Function Equals(ByVal obj As Object) As Boolean
        '    Return Equals(TryCast(obj, Sort))
        'End Function

        'Public Overloads Function Equals(ByVal s As Sort) As Boolean
        '    If s Is Nothing Then
        '        Return False
        '    Else
        '        Dim b As Boolean
        '        If Not String.IsNullOrEmpty(_custom) Then
        '            b = _custom = s._custom AndAlso _t Is s._t
        '        Else
        '            b = _f = s._f AndAlso _order = s._order AndAlso _ext = s._ext AndAlso _t Is s._t
        '        End If

        '        If b Then
        '            If Not _prev Is s._prev Then
        '                If _prev IsNot Nothing Then
        '                    b = _prev.Equals(s._prev)
        '                Else
        '                    b = False
        '                End If
        '            End If
        '        End If
        '        Return b
        '    End If
        'End Function

        Protected Overrides Function _Equals(ByVal p As Orm.SelectExpression) As Boolean
            Dim s As Sort = TryCast(p, Sort)
            If s Is Nothing Then
                Return False
            End If

            Dim b As Boolean = MyBase._Equals(s) AndAlso _order = s._order AndAlso _ext = s._ext

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
        End Function

        'Public Overrides Function GetHashCode() As Integer
        '    Return ToString.GetHashCode
        'End Function

        Protected ReadOnly Property Previous() As Sort
            Get
                Return _prev
            End Get
        End Property

        Public Overridable Function ExternalSort(Of T As {_IEntity})(ByVal mgr As OrmManager, ByVal schema As ObjectMappingEngine, ByVal objs As ReadOnlyObjectList(Of T)) As ReadOnlyObjectList(Of T)
            If Not IsExternal Then
                Throw New InvalidOperationException("Sort is not external")
            End If

            If Previous IsNot Nothing Then
                Throw New ArgumentException("Sort is linked")
            End If

            If _del IsNot Nothing Then
                Return CType(_del(mgr, schema, Me, objs), Global.Worm.ReadOnlyObjectList(Of T))
            Else
                Throw New InvalidOperationException("Delegate is not set")
            End If

        End Function

        Public Overridable Function Clone() As Object Implements System.ICloneable.Clone
            Dim s As New Sort(_prev, Type, Field, _order, _ext, _del)
            s.Computed = Computed
            s.Table = Table
            s.Values = Values
            s.Column = Column
            Return s
        End Function
    End Class

    Public Class OrmComparer(Of T As {_IEntity})
        Implements Generic.IComparer(Of T), IComparer

        Public Delegate Function GetObjectDelegate(ByVal x As T, ByVal t As Type) As _IEntity

        'Private _q As Generic.List(Of Sort)
        Private _mgr As OrmManager
        Private _s As Generic.Stack(Of Sort)
        Private _t As Type
        Private _getobj As GetObjectDelegate

        Public Sub New(ByVal t As Type, ByVal q As Generic.Stack(Of Sort))
            _s = q
            _mgr = OrmManager.CurrentManager
            _t = t
        End Sub

        Public Sub New(ByVal t As Type, ByVal s As Sort)
            _t = t
            _s = New Generic.Stack(Of Sort)
            _s.Push(s)
            _mgr = OrmManager.CurrentManager
        End Sub

        Public Sub New(ByVal s As Sort)
            _t = GetType(T)
            _s = New Generic.Stack(Of Sort)
            _s.Push(s)
            _mgr = OrmManager.CurrentManager
        End Sub

        Public Sub New(ByVal q As Generic.Stack(Of Sort))
            _s = q
            _mgr = OrmManager.CurrentManager
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
            _mgr = OrmManager.CurrentManager
            _t = GetType(T)
        End Sub

        Public Overridable Function Compare(ByVal x As T, ByVal y As T) As Integer Implements System.Collections.Generic.IComparer(Of T).Compare
            Dim p As Integer = 0
            For Each s As Sort In _s
                'If s.IsAny Then
                '    Throw New NotSupportedException("Any sorting is not supported")
                'End If
                Dim ss As IObjectSchemaBase = Nothing
                Dim xo As Object = GetValue(x, s, ss)
                Dim yo As Object = GetValue(y, s, ss)
                Dim pr2 As Pair(Of _IEntity, IOrmSorting) = TryCast(yo, Pair(Of _IEntity, IOrmSorting))
                If pr2 IsNot Nothing Then
                    Dim c As IComparer = pr2.Second.CreateSortComparer(s)
                    If c IsNot Nothing Then
                        p = c.Compare(CType(xo, Pair(Of _IEntity, IOrmSorting)).First, pr2.First)
                        If p = 0 Then
                            Continue For
                        Else
                            Exit For
                        End If
                    Else
                        Dim pr As Pair(Of _IEntity, IOrmSorting) = TryCast(xo, Pair(Of _IEntity, IOrmSorting))
                        xo = pr.First.GetValueOptimized(Nothing, New ColumnAttribute(s.FieldName), ss)
                        yo = pr2.First.GetValueOptimized(Nothing, New ColumnAttribute(s.FieldName), ss)
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

        Private Function GetValue(ByVal x As T, ByVal s As Sort, ByRef oschema As IObjectSchemaBase) As Object
            Dim xo As _IEntity = x
            If s.Type IsNot Nothing AndAlso _t IsNot s.Type Then
                Dim schema As ObjectMappingEngine = _mgr.MappingEngine
                If _getobj IsNot Nothing Then
                    xo = _getobj(x, s.Type)
                Else
                    xo = schema.GetJoinObj(oschema, xo, s.Type)
                End If
                If oschema Is Nothing Then
                    oschema = schema.GetObjectSchema(_t)
                End If
                Dim ss As IOrmSorting = TryCast(oschema, IOrmSorting)
                If ss IsNot Nothing Then
                    Return New Pair(Of IEntity, IOrmSorting)(xo, ss)
                Else
                    Return xo.GetValueOptimized(Nothing, New ColumnAttribute(s.FieldName), Nothing)
                End If
            End If
            Return xo.GetValueOptimized(Nothing, New ColumnAttribute(s.FieldName), oschema)
        End Function

        Private Function _Compare(ByVal x As Object, ByVal y As Object) As Integer Implements System.Collections.IComparer.Compare
            Return Compare(CType(TryCast(x, _IEntity), T), CType(TryCast(y, _IEntity), T))
        End Function
    End Class

End Namespace