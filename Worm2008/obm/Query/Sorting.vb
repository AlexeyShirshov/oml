Imports Worm.Entities
Imports Worm.Sorting
Imports Worm.Entities.Meta
Imports System.Collections.Generic
Imports Worm.Query

Namespace Sorting
    Public Enum SortType
        Asc
        Desc
    End Enum

    Public Delegate Function ExternalSortDelegate(ByVal mgr As OrmManager, _
        ByVal generator As ObjectMappingEngine, ByVal sort As Sort, ByVal objs As IList) As IEnumerable

    <Serializable()> _
    Public Class Sort
        Inherits SelectExpression
        Implements ICloneable

        'Private _f As String
        Friend _order As SortType
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

        Protected Friend Sub New(ByVal prev As Sort, ByVal os As EntityUnion, ByVal propertyAlias As String, ByVal order As SortType, ByVal external As Boolean, ByVal del As ExternalSortDelegate)
            MyBase.New(os, propertyAlias)
            '_f = fieldName
            _order = order
            _ext = external
            '_t = t
            _prev = prev
            _del = del
        End Sub

        Protected Friend Sub New(ByVal prev As Sort, ByVal t As Type, ByVal propertyAlias As String, ByVal order As SortType, ByVal external As Boolean, ByVal del As ExternalSortDelegate)
            MyBase.New(t, propertyAlias)
            '_f = fieldName
            _order = order
            _ext = external
            '_t = t
            _prev = prev
            _del = del
        End Sub

        'Public Sub New(ByVal t As Type, ByVal propertyAlias As String, ByVal order As SortType, ByVal external As Boolean)
        '    MyBase.New(t, propertyAlias)
        '    '_f = fieldName
        '    _order = order
        '    _ext = external
        '    '_t = t
        'End Sub

        Protected Friend Sub New(ByVal os As EntityUnion, ByVal propertyAlias As String, ByVal order As SortType, ByVal external As Boolean, ByVal del As ExternalSortDelegate)
            MyBase.New(os, propertyAlias)
            '_f = fieldName
            _order = order
            _ext = external
            '_t = t
            _del = del
        End Sub

        'Public Sub New(ByVal t As Type, ByVal propertyAlias As String, ByVal order As SortType, ByVal external As Boolean, ByVal del As ExternalSortDelegate)
        '    MyBase.New(t, propertyAlias)
        '    '_f = fieldName
        '    _order = order
        '    _ext = external
        '    '_t = t
        '    _del = del
        'End Sub

#End Region

#Region " Typeless ctors "

        Public Sub New(ByVal sortExpression As String, ByVal values() As FieldReference)
            '    '_t = t
            MyBase.New(sortExpression, values)
            '    _custom = sortExpression
            '    _values = values
        End Sub

        Public Sub New(ByVal prev As Sort, ByVal sortExpression As String, ByVal values() As FieldReference)
            MyBase.New(sortExpression, values)
            _prev = prev
            '_t = t
            '_custom = sortExpression
            '_values = values
        End Sub

        Public Sub New(ByVal t As Type, ByVal propertyAlias As String, ByVal order As SortType, ByVal external As Boolean)
            MyBase.New(t, propertyAlias)
            '_f = fieldName
            _order = order
            _ext = external
        End Sub

        Public Sub New(ByVal t As Type, ByVal propertyAlias As String, ByVal order As SortType, ByVal external As Boolean, ByVal del As ExternalSortDelegate)
            MyBase.New(t, propertyAlias)
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

        Protected Friend Sub New(ByVal prev As Sort, ByVal q As QueryCmd, ByVal order As SortType)
            MyBase.New(q)
            _order = order
            _prev = prev
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

        Public ReadOnly Property SortBy() As String
            Get
                If ObjectProperty.ObjectSource IsNot Nothing Then
                    Return ObjectProperty.Field
                Else
                    Return Column
                End If
            End Get
            'Set(ByVal value As String)
            '    PropertyAlias = value
            'End Set
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

        Protected Overrides Function _Equals(ByVal p As Entities.SelectExpression) As Boolean
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

        Public Overridable Function ExternalSort(Of T As {_IEntity})(ByVal mgr As OrmManager, _
            ByVal schema As ObjectMappingEngine, ByVal objs As IList(Of T)) As ReadOnlyObjectList(Of T)
            If Not IsExternal Then
                Throw New InvalidOperationException("Sort is not external")
            End If

            If Previous IsNot Nothing Then
                Throw New ArgumentException("Sort is linked")
            End If

            If _del IsNot Nothing Then
                Dim en As IEnumerable = _del(mgr, schema, Me, CType(objs, IList))
                If GetType(ReadOnlyObjectList(Of T)).IsAssignableFrom(en.GetType) Then
                    Return CType(en, ReadOnlyObjectList(Of T))
                Else
                    Return CType(OrmManager.CreateReadonlyList(GetType(T), en), ReadOnlyObjectList(Of T))
                End If
            Else
                Throw New InvalidOperationException("Delegate is not set")
            End If

        End Function

        Public Overridable Function Clone() As Object Implements System.ICloneable.Clone
            Dim s As New Sort(_prev, ObjectSource, PropertyAlias, _order, _ext, _del)
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

        Public Sub New(ByVal propertyAlias As String, ByVal st As SortType)
            '_q = New Generic.List(Of Sort)
            _s = New Generic.Stack(Of Sort)
            _s.Push(New Sort(GetType(T), propertyAlias, st, False))
            _mgr = OrmManager.CurrentManager
            _t = GetType(T)
        End Sub

        Public Overridable Function Compare(ByVal x As T, ByVal y As T) As Integer Implements System.Collections.Generic.IComparer(Of T).Compare
            Dim p As Integer = 0
            Dim tos As IEntitySchema = _mgr.MappingEngine.GetEntitySchema(_t)
            For Each s As Sort In _s
                'If s.IsAny Then
                '    Throw New NotSupportedException("Any sorting is not supported")
                'End If
                Dim ss As IEntitySchema = _mgr.MappingEngine.GetEntitySchema(s.ObjectSource.GetRealType(_mgr.MappingEngine))
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
                        xo = _mgr.MappingEngine.GetPropertyValue(pr.First, s.SortBy, ss)
                        yo = _mgr.MappingEngine.GetPropertyValue(pr2.First, s.SortBy, ss)
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
                            Throw New InvalidOperationException("Value " & s.SortBy & " of type " & s.ObjectSource.ToStaticString(_mgr.MappingEngine, _mgr.GetContextFilter) & " is not supported IComparable")
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

        Private Function GetValue(ByVal x As T, ByVal s As Sort, ByVal oschema As IEntitySchema) As Object
            Dim xo As _IEntity = x
            Dim schema As ObjectMappingEngine = _mgr.MappingEngine
            Dim st As Type = s.ObjectSource.GetRealType(schema)
            If st IsNot Nothing AndAlso _t IsNot st Then
                If _getobj IsNot Nothing Then
                    xo = _getobj(x, st)
                Else
                    xo = schema.GetJoinObj(oschema, xo, st)
                End If
                Dim os As IEntitySchema = schema.GetEntitySchema(_t)
                Dim ss As IOrmSorting = TryCast(os, IOrmSorting)
                If ss IsNot Nothing Then
                    Return New Pair(Of IEntity, IOrmSorting)(xo, ss)
                Else
                    Return schema.GetPropertyValue(xo, s.SortBy, oschema) 'xo.GetValueOptimized(Nothing, s.SortBy, Nothing)
                End If
            End If
            Return schema.GetPropertyValue(xo, s.SortBy, oschema) 'xo.GetValueOptimized(Nothing, s.SortBy, oschema)
        End Function

        Private Function _Compare(ByVal x As Object, ByVal y As Object) As Integer Implements System.Collections.IComparer.Compare
            Return Compare(CType(TryCast(x, _IEntity), T), CType(TryCast(y, _IEntity), T))
        End Function
    End Class

End Namespace