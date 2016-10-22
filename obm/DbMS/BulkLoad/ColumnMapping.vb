Namespace Database
    Public Class ColumnMapping
        ' Methods
        Public Sub New()

        End Sub
        Public Sub New(ByVal sourceColumnOrdinal As Integer, ByVal destinationColumnOrdinal As Integer)
            SourceOrdinal = sourceColumnOrdinal
            DestinationOrdinal = destinationColumnOrdinal
        End Sub
        Public Sub New(ByVal sourceColumnOrdinal As Integer, ByVal destinationColumn As String)
            SourceOrdinal = sourceColumnOrdinal
            Me.DestinationColumn = destinationColumn
        End Sub
        Public Sub New(ByVal sourceColumn As String, ByVal destinationColumnOrdinal As Integer)
            Me.SourceColumn = sourceColumn
            DestinationOrdinal = destinationColumnOrdinal
        End Sub
        Public Sub New(ByVal sourceColumn As String, ByVal destinationColumn As String)
            Me.SourceColumn = sourceColumn
            Me.DestinationColumn = destinationColumn
        End Sub

        ' Properties
        Public Property DestinationColumn As String
        Public Property DestinationOrdinal As Integer?
        Public Property SourceColumn As String
        Public Property SourceOrdinal As Integer?
    End Class


End Namespace