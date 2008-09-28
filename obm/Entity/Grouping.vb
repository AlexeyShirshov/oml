Imports Worm.Orm.Meta

Namespace Orm
    Public Class Grouping
        Inherits OrmProperty

        Public Enum SummaryValues
            None
            Cube
            Rollup
        End Enum

        Private _cube As SummaryValues
        Private _all As Boolean

        Public Sub New(ByVal p As OrmProperty)
            MyBase.New(p.Computed, p.Values, p.Column)
            Type = p.Type
            Table = p.Table
            Field = p.Field
        End Sub

        Public Sub New(ByVal t As Type, ByVal field As String)
            MyBase.New(t, field)
        End Sub

        Public Sub New(ByVal t As SourceFragment, ByVal column As String)
            MyBase.New(t, column)
        End Sub

        Public Sub New(ByVal t As SourceFragment, ByVal column As String, ByVal field As String)
            MyBase.new(t, column, field)
        End Sub

        Public Sub New(ByVal computed As String, ByVal values() As Pair(Of Object, String), ByVal [alias] As String)
            MyBase.New(computed, values, [alias])
        End Sub

        Public Property SumValues() As SummaryValues
            Get
                Return _cube
            End Get
            Set(ByVal value As SummaryValues)
                _cube = value
                RaiseOnChange()
            End Set
        End Property

        Public Property All() As Boolean
            Get
                Return _all
            End Get
            Set(ByVal value As Boolean)
                _all = value
                RaiseOnChange()
            End Set
        End Property

        Protected Overrides Function _Equals(ByVal p As OrmProperty) As Boolean
            Dim g As Grouping = TryCast(p, Grouping)
            If g Is Nothing Then
                Return False
            End If

            Return MyBase._Equals(g) AndAlso _cube = g._cube AndAlso _all = g._all
        End Function

        Public Overrides Function ToString() As String
            Return MyBase.ToString() & _cube & _all
        End Function
    End Class
End Namespace