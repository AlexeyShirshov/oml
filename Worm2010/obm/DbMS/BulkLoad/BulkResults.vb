Namespace Database
    Public Class BulkResults
        Implements IBulkLoadResults

        Private _added As Integer

        Sub New(added As Integer)
            _added = added
        End Sub

        Public ReadOnly Property Loaded As Integer Implements IBulkLoadResults.Loaded
            Get
                Return _added
            End Get
        End Property
    End Class
End Namespace