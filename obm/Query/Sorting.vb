Imports Worm.Entities
Imports Worm.Query.Sorting
Imports Worm.Entities.Meta
Imports System.Collections.Generic
Imports Worm.Query
Imports Worm.Criteria.Values

Namespace Query
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
            Private Shared _empty As Sort = New Sort
            Private _key As Integer

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

            Public Sub New(ByVal sortExpression As String, ByVal values() As IFilterValue)
                '    '_t = t
                MyBase.New(sortExpression, values)
                '    _custom = sortExpression
                '    _values = values
            End Sub

            Public Sub New(ByVal prev As Sort, ByVal sortExpression As String, ByVal values() As IFilterValue)
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

            Protected Friend Sub New(ByVal prev As Sort, ByVal q As Query.QueryCmd)
                MyBase.New(q)
                _prev = prev
            End Sub

            Protected Friend Sub New(ByVal prev As Sort)
                _prev = prev
            End Sub

            Protected Friend Sub New()
            End Sub

            'Protected Friend Sub New(ByVal prev As SortLink, ByVal custom As CustomValue)
            '    MyBase.New(custom)
            '    _prev = prev
            'End Sub

            Protected Friend Sub New(ByVal custom As CustomValue)
                MyBase.New(custom)
            End Sub

            'Protected Sub RaiseOnChange()
            '    RaiseEvent OnChange()
            'End Sub

            'Public Shared ReadOnly Property Empty() As Sort
            '    Get
            '        Return _empty
            '    End Get
            'End Property
            Public ReadOnly Property CanEvaluate() As Boolean
                Get
                    For Each s As Sort In New Iterator(Me)
                        If s.IsCustom OrElse s.Query IsNot Nothing Then
                            Return False
                        End If
                    Next
                    Return True
                End Get
            End Property

            Public Property CustomSortExpression() As String
                Get
                    Return Computed
                End Get
                Set(ByVal value As String)
                    Computed = value
                End Set
            End Property

            Public Function GetOnlyKey(ByVal schema As ObjectMappingEngine, ByVal contextFilter As Object) As Sort
                Dim s As New Sort
                s._key = GetStaticString(schema, contextFilter).GetHashCode
                Return s
            End Function

            Public ReadOnly Property Key() As Integer
                Get
                    Return _key
                End Get
            End Property

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
                    If Not String.IsNullOrEmpty(ObjectProperty.PropertyAlias) Then
                        Return ObjectProperty.PropertyAlias
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
                Set(ByVal value As SortType)
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

            Protected Overrides Function _Equals(ByVal p As SelectExpression) As Boolean
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

            Public Property Previous() As Sort
                Get
                    Return _prev
                End Get
                Set(ByVal value As Sort)
                    _prev = value
                End Set
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
                        Return CType(OrmManager._CreateReadOnlyList(GetType(T), en), ReadOnlyObjectList(Of T))
                    End If
                Else
                    Throw New InvalidOperationException("Delegate is not set")
                End If

            End Function

            Public Overridable Function Clone() As Object Implements System.ICloneable.Clone
                If _key <> 0 Then
                    Return Me
                Else
                    Dim s As New Sort()
                    CopyTo(s)
                    With s
                        ._order = _order
                        ._ext = _ext
                        ._del = _del
                        ._prev = _prev
                    End With
                    Return s
                End If
            End Function

            Public Function CreateComparer(Of T As _IEntity)(ByVal rt As Type) As OrmComparer(Of T)
                Dim q As New Stack(Of Sort)
                If Not IsExternal Then
                    For Each ns As Sort In New Sort.Iterator(Me)
                        If ns.IsExternal Then
                            Throw New ObjectMappingException("External sort must be alone")
                        End If
                        If ns.IsCustom Then
                            Throw New ObjectMappingException("Custom sort is not supported")
                        End If
                        q.Push(ns)
                    Next

                    Return New OrmComparer(Of T)(rt, q)
                End If
                Return Nothing
            End Function
        End Class

        Public Class OrmComparer(Of T As {_IEntity})
            Implements Generic.IComparer(Of T), IComparer

            Public Delegate Function GetObjectDelegate(ByVal x As T, ByVal t As Type) As _IEntity

            'Private _q As Generic.List(Of Sort)
            'Private _mgr As OrmManager
            Private _s As Generic.Stack(Of Sort)
            Private _t As Type
            Private _getobj As GetObjectDelegate

            Public Sub New(ByVal t As Type, ByVal q As Generic.Stack(Of Sort))
                _s = q
                '_mgr = OrmManager.CurrentManager
                _t = t
            End Sub

            Public Sub New(ByVal t As Type, ByVal s As Sort)
                _t = t
                _s = New Generic.Stack(Of Sort)
                _s.Push(s)
                '_mgr = OrmManager.CurrentManager
            End Sub

            Public Sub New(ByVal s As Sort)
                _t = GetType(T)
                _s = New Generic.Stack(Of Sort)
                _s.Push(s)
                '_mgr = OrmManager.CurrentManager
            End Sub

            Public Sub New(ByVal q As Generic.Stack(Of Sort))
                _s = q
                '_mgr = OrmManager.CurrentManager
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
                '_mgr = OrmManager.CurrentManager
                _t = GetType(T)
            End Sub

            Public Overridable Function Compare(ByVal x As T, ByVal y As T) As Integer Implements System.Collections.Generic.IComparer(Of T).Compare
                Dim p As Integer = 0
                Dim tos As IEntitySchema = x.GetMappingEngine.GetEntitySchema(_t)
                For Each s As Sort In _s
                    'If s.IsAny Then
                    '    Throw New NotSupportedException("Any sorting is not supported")
                    'End If
                    Dim ss As IEntitySchema = x.GetMappingEngine.GetEntitySchema(s.ObjectSource.GetRealType(x.GetMappingEngine))
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
                            xo = x.GetMappingEngine.GetPropertyValue(pr.First, s.SortBy, ss)
                            yo = x.GetMappingEngine.GetPropertyValue(pr2.First, s.SortBy, ss)
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
                                Throw New InvalidOperationException("Value " & s.SortBy & " in type " & s.ObjectSource.ToStaticString(x.GetMappingEngine, Nothing) & " is not supported IComparable")
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
                Dim schema As ObjectMappingEngine = x.GetMappingEngine
                Dim st As Type = s.ObjectSource.GetRealType(schema)
                If st IsNot Nothing AndAlso _t IsNot st Then
                    If _getobj IsNot Nothing Then
                        xo = _getobj(x, st)
                    Else
                        xo = schema.GetJoinObj(oschema, xo, st)
                    End If
                    Dim os As IEntitySchema = schema.GetEntitySchema(st)
                    Dim ss As IOrmSorting = TryCast(os, IOrmSorting)
                    If ss IsNot Nothing Then
                        Return New Pair(Of _IEntity, IOrmSorting)(xo, ss)
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
End Namespace