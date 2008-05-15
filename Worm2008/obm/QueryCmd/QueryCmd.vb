Imports System.Collections.Generic
Imports Worm.Orm.Meta
Imports Worm.Criteria.Core
Imports Worm.Sorting
Imports Worm.Orm
Imports Worm.Criteria.Joins

Namespace Query

    Public Interface IExecutor
        Function Exec(Of ReturnType As {OrmBase, New})(ByVal mgr As OrmManagerBase, ByVal query As QueryCmdBase) As ReadOnlyList(Of ReturnType)
    End Interface

    Public Class Top
        Private _perc As Boolean
        Private _ties As Boolean
        Private _n As Integer

        Public Sub New(ByVal n As Integer)
            _n = n
        End Sub

        Public Sub New(ByVal n As Integer, ByVal percent As Boolean)
            MyClass.New(n)
            _perc = percent
        End Sub

        Public Sub New(ByVal n As Integer, ByVal percent As Boolean, ByVal ties As Boolean)
            MyClass.New(n, percent)
            _ties = ties
        End Sub

        Public ReadOnly Property Percent() As Boolean
            Get
                Return _perc
            End Get
        End Property

        Public ReadOnly Property Ties() As Boolean
            Get
                Return _ties
            End Get
        End Property

        Public ReadOnly Property Count() As Integer
            Get
                Return _n
            End Get
        End Property

        Public Function GetDynamicKey() As String
            Return "-top-" & _n.ToString & "-"
        End Function

        Public Function GetStaticKey() As String
            Return "-top-"
        End Function
    End Class

    Public Class QueryCmdBase
        Protected _fields As List(Of OrmProperty)
        Protected _filter As IGetFilter
        Protected _group As List(Of OrmProperty)
        Protected _order As Sort
        Protected _aggregates As List(Of AggregateBase)
        Protected _load As Boolean
        Protected _top As Top
        Protected _page As Nullable(Of Integer)
        Protected _distinct As Boolean
        Protected _dontcache As Boolean
        Protected _clientPage As Pair(Of Integer)
        Protected _joins() As OrmJoin
        Protected _autoJoins As Boolean
        Protected _table As OrmTable

        Public Function GetStaticKey(ByVal mgr As OrmManagerBase, ByVal j As List(Of OrmJoin), ByVal f As IFilter) As String
            Dim key As String = String.Empty

            If f IsNot Nothing Then
                key &= f.ToStaticString & "$"
            End If

            If j IsNot Nothing Then
                For Each join As OrmJoin In j
                    If Not OrmJoin.IsEmpty(join) Then
                        key &= join.ToString
                    End If
                Next
            End If

            If _top IsNot Nothing Then
                key &= _top.GetStaticKey & "$"
            End If

            'key &= mgr.ObjectSchema.GetEntityKey(mgr.GetFilterInfo, GetType(T))

            Return key & "$" & mgr.GetStaticKey()
        End Function

        Public Function GetDynamicKey(ByVal j As List(Of OrmJoin), ByVal f As IFilter) As String
            Dim id As String = String.Empty

            If f IsNot Nothing Then
                id &= f.ToString & "$"
            End If

            If j IsNot Nothing Then
                For Each join As OrmJoin In j
                    If Not OrmJoin.IsEmpty(join) Then
                        id &= join.ToString
                    End If
                Next
            End If

            If _top IsNot Nothing Then
                id &= _top.GetDynamicKey & "$"
            End If

            Return id '& GetType(T).ToString
        End Function

#Region " Properties "

        Public ReadOnly Property Table() As OrmTable
            Get
                Return _table
            End Get
        End Property

        Public Property ClientPaging() As Pair(Of Integer)
            Get
                Return _clientPage
            End Get
            Set(ByVal value As Pair(Of Integer))
                _clientPage = value
            End Set
        End Property

        Public Property DontCache() As Boolean
            Get
                Return _dontcache
            End Get
            Set(ByVal value As Boolean)
                _dontcache = value
            End Set
        End Property

        Public Property AutoJoins() As Boolean
            Get
                Return _autoJoins
            End Get
            Set(ByVal value As Boolean)
                _autoJoins = value
            End Set
        End Property

        Public ReadOnly Property Group() As List(Of OrmProperty)
            Get
                Return _group
            End Get
        End Property

        Public ReadOnly Property Joins() As OrmJoin()
            Get
                Return _joins
            End Get
        End Property

        Public ReadOnly Property Aggregates() As List(Of AggregateBase)
            Get
                Return _aggregates
            End Get
        End Property

        Public ReadOnly Property SelectList() As List(Of OrmProperty)
            Get
                Return _fields
            End Get
        End Property

        Public ReadOnly Property Sort() As Sort
            Get
                Return _order
            End Get
        End Property

        Public Property Top() As Top
            Get
                Return _top
            End Get
            Set(ByVal value As Top)
                _top = value
            End Set
        End Property

        Public Property Distinct() As Boolean
            Get
                Return _distinct
            End Get
            Set(ByVal value As Boolean)
                _distinct = value
            End Set
        End Property

        Public Property Filter() As IGetFilter
            Get
                Return _filter
            End Get
            Set(ByVal value As IGetFilter)
                _filter = value
            End Set
        End Property

        Public Property WithLoad() As Boolean
            Get
                Return _load
            End Get
            Set(ByVal value As Boolean)
                _load = value
            End Set
        End Property
#End Region

    End Class

    Public Class QueryCmd(Of ReturnType As {OrmBase, New})
        Inherits QueryCmdBase

        Private _exec As IExecutor

        Protected Function GetExecutor(ByVal mgr As OrmManagerBase) As IExecutor
            'If _dontcache Then
            If _exec Is Nothing Then
                _exec = mgr.ObjectSchema.CreateExecutor()
            End If
            Return _exec
            'Else
            'Return mgr.ObjectSchema.CreateExecutor()
            'End If
        End Function

        Public Function Exec(ByVal mgr As OrmManagerBase) As ReadOnlyList(Of ReturnType)
            Return GetExecutor(mgr).Exec(Of ReturnType)(mgr, Me)
        End Function

        Public Shared Function Create(ByVal filter As IGetFilter) As QueryCmd(Of ReturnType)
            Return Create(filter, Nothing, False)
        End Function

        Public Shared Function Create(ByVal table As OrmTable, ByVal field As String) As QueryCmd(Of ReturnType)
            Dim q As QueryCmd(Of ReturnType) = Create(table, Nothing, Nothing, False)
            q._fields = New List(Of OrmProperty)
            q._fields.Add(New OrmProperty(table, field))
            Return q
        End Function

        Public Shared Function Create(ByVal table As OrmTable, ByVal fields() As String) As QueryCmd(Of ReturnType)
            Dim q As QueryCmd(Of ReturnType) = Create(table, Nothing, Nothing, False)
            q._fields = New List(Of OrmProperty)
            For Each f As String In fields
                q._fields.Add(New OrmProperty(table, f))
            Next
            Return q
        End Function

        Public Shared Function Create(ByVal table As OrmTable, ByVal fields() As OrmProperty) As QueryCmd(Of ReturnType)
            Dim q As QueryCmd(Of ReturnType) = Create(table, Nothing, Nothing, False)
            q._fields = New List(Of OrmProperty)
            q._fields.AddRange(fields)
            Return q
        End Function

        Public Shared Function Create(ByVal filter As IGetFilter, ByVal sort As Sort) As QueryCmd(Of ReturnType)
            Return Create(filter, sort, False)
        End Function

        Public Shared Function Create(ByVal table As OrmTable, ByVal filter As IGetFilter, ByVal sort As Sort) As QueryCmd(Of ReturnType)
            Return Create(table, filter, sort, False)
        End Function

        Public Shared Function Create(ByVal filter As IGetFilter, ByVal sort As Sort, ByVal withLoad As Boolean) As QueryCmd(Of ReturnType)
            Dim q As New QueryCmd(Of ReturnType)
            With q
                ._filter = filter
                ._order = sort
                ._load = withLoad
            End With
            Return q
        End Function

        Public Shared Function Create(ByVal table As OrmTable, ByVal filter As IGetFilter, ByVal sort As Sort, ByVal withLoad As Boolean) As QueryCmd(Of ReturnType)
            Dim q As QueryCmd(Of ReturnType) = Create(filter, sort, withLoad)
            q._table = table
            Return q
        End Function
#Region " Extensions "
        Public Function [Single](ByVal mgr As OrmManagerBase) As ReturnType
            Dim r As ReadOnlyList(Of ReturnType) = Exec(mgr)
            If r.Count <> 1 Then
                Throw New InvalidOperationException
            Else
                Return r(0)
            End If
        End Function

        Public Function ToList(ByVal mgr As OrmManagerBase) As ReadOnlyList(Of ReturnType)
            Return Exec(mgr)
        End Function
#End Region
    End Class

End Namespace