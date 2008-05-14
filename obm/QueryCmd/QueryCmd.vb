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

    Public MustInherit Class Top
        Private _perc As Boolean
        Private _ties As Boolean
        Private _n As Integer

        Public MustOverride Function MakeStmt(ByVal s As QueryGenerator) As String
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

        Public Function GetStaticKey(ByVal j As List(Of OrmJoin), ByVal f As IFilter) As String

        End Function

        Public Function GetDynamicKey(ByVal j As List(Of OrmJoin), ByVal f As IFilter) As String

        End Function

#Region " Properties "

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

        Public ReadOnly Property Filter() As IGetFilter
            Get
                Return _filter
            End Get
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
            If _dontcache Then
                If _exec Is Nothing Then
                    _exec = mgr.ObjectSchema.CreateExecutor()
                End If
                Return _exec
            Else
                Return mgr.ObjectSchema.CreateExecutor()
            End If
        End Function

        Public Function Exec(ByVal mgr As OrmManagerBase) As ReadOnlyList(Of ReturnType)
            Return GetExecutor(mgr).Exec(Of ReturnType)(mgr, Me)
        End Function

        Public Shared Function Create(ByVal filter As IGetFilter) As QueryCmd(Of ReturnType)
            Return Create(filter, Nothing, False)
        End Function

        Public Shared Function Create(ByVal filter As IGetFilter, ByVal sort As Sort) As QueryCmd(Of ReturnType)
            Return Create(filter, sort, False)
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
    End Class

End Namespace