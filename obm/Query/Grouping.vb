Imports Worm.Entities.Meta

Namespace Query
    Public Class Grouping
        Inherits SelectExpression

        Public Enum SummaryValues
            None
            Cube
            Rollup
        End Enum

        Private _cube As SummaryValues
        Private _all As Boolean

        Public Sub New(ByVal p As SelectExpression)
            p.CopyTo(Me)
        End Sub

        Public Sub New(ByVal t As Type, ByVal field As String)
            MyBase.New(t, field)
        End Sub

        Public Sub New(ByVal t As SourceFragment, ByVal column As String)
            MyBase.New(t, column)
        End Sub

        Public Sub New(ByVal t As SourceFragment, ByVal column As String, ByVal fieldAlias As String)
            MyBase.new(t, column, fieldAlias)
        End Sub

        'Public Sub New(ByVal computed As String, ByVal [alias] As String, ByVal ParamArray values() As IFilterValue)
        '    MyBase.New(computed, values, [alias])
        'End Sub

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

        Protected Overrides Function _Equals(ByVal p As SelectExpression) As Boolean
            Dim g As Grouping = TryCast(p, Grouping)
            If g Is Nothing Then
                Return False
            End If

            Return MyBase._Equals(g) AndAlso _cube = g._cube AndAlso _all = g._all
        End Function

        Public Overrides Function _ToString() As String
            Return MyBase._ToString() & _cube & _all
        End Function

        Public Overrides Function GetStaticString(ByVal mpe As ObjectMappingEngine, ByVal contextFilter As Object) As String
            Return MyBase.GetStaticString(mpe, contextFilter) & _cube & _all
        End Function
    End Class
End Namespace